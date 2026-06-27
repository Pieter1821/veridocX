namespace VeridocX.Server.Domain.Affordability;

public enum LivingExpenseBasis { Declared, MinimumNorm }

public record ExpenseLine(string Description, decimal Amount);

public record LivingExpenses
{
    public decimal Accommodation { get; init; }
    public decimal Transport { get; init; }
    public decimal Food { get; init; }
    public decimal Education { get; init; }
    public decimal Medical { get; init; }
    public decimal WaterAndElectricity { get; init; }
    public decimal Maintenance { get; init; }

    public decimal Total =>
        Accommodation + Transport + Food + Education + Medical + WaterAndElectricity + Maintenance;
}

public record AffordabilityInput
{
    public decimal FixedGrossSalary { get; init; }
    public decimal AverageOvertime { get; init; }
    public decimal PayslipDeductions { get; init; }

    public LivingExpenses DeclaredLivingExpenses { get; init; } = new();
    public IReadOnlyList<ExpenseLine> CreditBureauObligations { get; init; } = [];
    public IReadOnlyList<ExpenseLine> BankStatementExpenses { get; init; } = [];

    public bool ExpenseQuestionnaireCompleted { get; init; }

    public decimal ProposedInstalment { get; init; }
}

public record AffordabilityStep(string Name, decimal Amount, string Detail);

public record AffordabilityResult
{
    public decimal GrossMonthlyIncome { get; init; }
    public decimal PayslipDeductions { get; init; }
    public decimal TakeHomeIncome { get; init; }

    public decimal DeclaredLivingExpenses { get; init; }
    public decimal MinimumExpenseNorm { get; init; }
    public decimal LivingExpensesApplied { get; init; }
    public LivingExpenseBasis LivingExpenseBasis { get; init; }
    public bool DeclaredBelowNorm { get; init; }
    public bool ExpenseQuestionnaireCompleted { get; init; }

    public decimal CreditBureauObligations { get; init; }
    public decimal BankStatementExpenses { get; init; }

    public decimal DiscretionaryIncome { get; init; }
    public decimal MaxAffordableInstalment { get; init; }
    public decimal ProposedInstalment { get; init; }
    public bool IsAffordable { get; init; }

    public bool IsRecklessIfGranted { get; init; }
    public IReadOnlyList<string> RecklessReasons { get; init; } = [];

    public decimal DebtToIncomeRatio { get; init; }
    public decimal InstalmentToNetIncomeRatio { get; init; }
    public decimal DiscretionaryBufferRatio { get; init; }

    public IReadOnlyList<AffordabilityStep> Steps { get; init; } = [];
}
