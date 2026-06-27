namespace VeridocX.Server.Domain.Affordability;

public static class AffordabilityCalculator
{
    // Source of truth: National Credit Act, Regulation 23A(9) Minimum Expense Norms.
    private static readonly (decimal Threshold, decimal FixedFactor, decimal Rate)[] Bands =
    [
        (0m, 0m, 1.00m),
        (800m, 800m, 0.0675m),
        (6250m, 1167.88m, 0.09m),
        (25000m, 2855.38m, 0.082m),
        (50000m, 4905.38m, 0.0675m)
    ];

    public static decimal MinimumLivingExpenses(decimal grossMonthlyIncome)
    {
        var band = Bands[0];
        foreach (var candidate in Bands)
        {
            if (grossMonthlyIncome >= candidate.Threshold)
                band = candidate;
        }

        return band.FixedFactor + (grossMonthlyIncome - band.Threshold) * band.Rate;
    }

    public static AffordabilityResult Assess(AffordabilityInput input)
    {
        var gross = input.FixedGrossSalary + input.AverageOvertime;
        var takeHome = gross - input.PayslipDeductions;

        var declaredLiving = input.DeclaredLivingExpenses.Total;
        var norm = MinimumLivingExpenses(gross);
        var declaredBelowNorm = declaredLiving < norm;

        // Reg 23A(11): a lower-than-norm declaration may only be used when the consumer
        // completes the prescribed questionnaire. Otherwise the norm is the binding floor.
        var acceptLowerDeclaration = declaredBelowNorm && input.ExpenseQuestionnaireCompleted;
        var basis = declaredBelowNorm && !acceptLowerDeclaration
            ? LivingExpenseBasis.MinimumNorm
            : LivingExpenseBasis.Declared;
        var livingApplied = basis == LivingExpenseBasis.MinimumNorm ? norm : declaredLiving;

        var bureau = Sum(input.CreditBureauObligations);
        var bank = Sum(input.BankStatementExpenses);

        var discretionary = takeHome - livingApplied - bureau - bank;
        var maxAffordableInstalment = discretionary > 0m ? discretionary : 0m;
        var isAffordable = discretionary >= input.ProposedInstalment;

        var recklessReasons = new List<string>();
        if (discretionary < 0m)
            recklessReasons.Add(
                $"Over-indebted: discretionary income is negative ({discretionary:0.00}) before the new instalment.");
        if (discretionary < input.ProposedInstalment)
            recklessReasons.Add(
                $"Discretionary income ({discretionary:0.00}) does not cover the proposed instalment ({input.ProposedInstalment:0.00}).");
        if (declaredBelowNorm && !input.ExpenseQuestionnaireCompleted)
            recklessReasons.Add(
                "Declared living expenses are below the Reg 23A norm and no questionnaire was completed; norm applied.");

        var isReckless = !isAffordable;

        var debtToIncomeRatio = Ratio(bureau + input.ProposedInstalment, gross);
        var instalmentToNetIncomeRatio = Ratio(input.ProposedInstalment, takeHome);
        var discretionaryBufferRatio = Ratio(discretionary, takeHome);

        var livingDetail = basis switch
        {
            LivingExpenseBasis.MinimumNorm =>
                $"declared {declaredLiving} below Reg 23A norm {norm}; norm applied (no questionnaire)",
            _ when declaredBelowNorm =>
                $"declared {declaredLiving} below norm {norm}; accepted via completed questionnaire",
            _ => "declared living expenses"
        };

        var steps = new List<AffordabilityStep>
        {
            new("gross_income", gross, "fixed salary + overtime"),
            new("payslip_deductions", -input.PayslipDeductions, "tax, UIF and other"),
            new("take_home_income", takeHome, "net income"),
            new("living_expenses", -livingApplied, livingDetail),
            new("credit_bureau", -bureau, "existing loan instalments"),
            new("bank_statement", -bank, "recurring expenses not on bureau"),
            new("discretionary_income", discretionary, "available to fund new credit"),
            new("proposed_instalment", input.ProposedInstalment, "requested new credit instalment")
        };

        return new AffordabilityResult
        {
            GrossMonthlyIncome = gross,
            PayslipDeductions = input.PayslipDeductions,
            TakeHomeIncome = takeHome,
            DeclaredLivingExpenses = declaredLiving,
            MinimumExpenseNorm = norm,
            LivingExpensesApplied = livingApplied,
            LivingExpenseBasis = basis,
            DeclaredBelowNorm = declaredBelowNorm,
            ExpenseQuestionnaireCompleted = input.ExpenseQuestionnaireCompleted,
            CreditBureauObligations = bureau,
            BankStatementExpenses = bank,
            DiscretionaryIncome = discretionary,
            MaxAffordableInstalment = maxAffordableInstalment,
            ProposedInstalment = input.ProposedInstalment,
            IsAffordable = isAffordable,
            IsRecklessIfGranted = isReckless,
            RecklessReasons = recklessReasons,
            DebtToIncomeRatio = debtToIncomeRatio,
            InstalmentToNetIncomeRatio = instalmentToNetIncomeRatio,
            DiscretionaryBufferRatio = discretionaryBufferRatio,
            Steps = steps
        };
    }

    private static decimal Sum(IReadOnlyList<ExpenseLine> lines)
    {
        decimal total = 0m;
        foreach (var line in lines)
            total += line.Amount;
        return total;
    }

    private static decimal Ratio(decimal numerator, decimal denominator) =>
        denominator > 0m ? Math.Round(numerator / denominator, 4) : 0m;
}
