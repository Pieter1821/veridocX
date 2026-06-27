using VeridocX.Server.Domain.Affordability;
using Xunit;

namespace VeridocX.Tests;

public class AffordabilityCalculatorTests
{
    [Theory]
    [InlineData(500, 500)]
    [InlineData(5000, 1083.50)]
    [InlineData(7000, 1235.38)]
    [InlineData(30000, 3265.38)]
    [InlineData(100000, 8280.38)]
    public void Minimum_living_expenses_follow_reg_23A_table(decimal grossIncome, decimal expected)
    {
        Assert.Equal(expected, AffordabilityCalculator.MinimumLivingExpenses(grossIncome));
    }

    [Fact]
    public void Builds_gross_and_take_home_from_payslip_parts()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 28000m,
            AverageOvertime = 2000m,
            PayslipDeductions = 6000m
        });

        Assert.Equal(30000m, result.GrossMonthlyIncome);
        Assert.Equal(24000m, result.TakeHomeIncome);
    }

    [Fact]
    public void Affordable_when_discretionary_income_covers_instalment()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses
            {
                Accommodation = 6000m,
                Transport = 2000m,
                Food = 3000m,
                WaterAndElectricity = 1000m
            },
            CreditBureauObligations = [new ExpenseLine("Vehicle finance", 4000m)],
            BankStatementExpenses = [new ExpenseLine("Insurance", 1000m)],
            ProposedInstalment = 5000m
        });

        Assert.Equal(12000m, result.DeclaredLivingExpenses);
        Assert.Equal(7000m, result.DiscretionaryIncome);
        Assert.True(result.IsAffordable);
        Assert.False(result.DeclaredBelowNorm);
    }

    [Fact]
    public void Declared_living_below_norm_is_flagged_and_norm_applied()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses { Food = 2000m },
            CreditBureauObligations = [new ExpenseLine("Store card", 4000m)],
            ProposedInstalment = 5000m
        });

        Assert.True(result.DeclaredBelowNorm);
        Assert.Equal(3265.38m, result.MinimumExpenseNorm);
        Assert.Equal(3265.38m, result.LivingExpensesApplied);
        Assert.Equal(16734.62m, result.DiscretionaryIncome);
    }

    [Fact]
    public void Completed_questionnaire_allows_lower_declared_expenses()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses { Food = 2000m },
            ExpenseQuestionnaireCompleted = true,
            ProposedInstalment = 5000m
        });

        Assert.True(result.DeclaredBelowNorm);
        Assert.Equal(LivingExpenseBasis.Declared, result.LivingExpenseBasis);
        Assert.Equal(2000m, result.LivingExpensesApplied);
        Assert.Equal(22000m, result.DiscretionaryIncome);
    }

    [Fact]
    public void Max_affordable_instalment_equals_discretionary_income()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses { Food = 2000m },
            CreditBureauObligations = [new ExpenseLine("Store card", 4000m)],
            ProposedInstalment = 5000m
        });

        Assert.Equal(result.DiscretionaryIncome, result.MaxAffordableInstalment);
    }

    [Fact]
    public void Not_affordable_when_instalment_exceeds_discretionary_income()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 8000m,
            PayslipDeductions = 1200m,
            DeclaredLivingExpenses = new LivingExpenses { Accommodation = 2000m, Food = 1500m },
            CreditBureauObligations = [new ExpenseLine("Personal loan", 3500m)],
            ProposedInstalment = 3000m
        });

        Assert.False(result.IsAffordable);
    }

    [Fact]
    public void Unaffordable_grant_is_flagged_reckless_with_reasons()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 8000m,
            PayslipDeductions = 1200m,
            DeclaredLivingExpenses = new LivingExpenses { Accommodation = 2000m, Food = 1500m },
            CreditBureauObligations = [new ExpenseLine("Personal loan", 3500m)],
            ProposedInstalment = 3000m
        });

        Assert.True(result.IsRecklessIfGranted);
        Assert.NotEmpty(result.RecklessReasons);
    }

    [Fact]
    public void Affordable_grant_is_not_reckless()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses
            {
                Accommodation = 6000m, Transport = 2000m, Food = 3000m, WaterAndElectricity = 1000m
            },
            CreditBureauObligations = [new ExpenseLine("Vehicle finance", 4000m)],
            ProposedInstalment = 5000m
        });

        Assert.False(result.IsRecklessIfGranted);
        Assert.Empty(result.RecklessReasons);
    }

    [Fact]
    public void Risk_ratios_are_computed()
    {
        var result = AffordabilityCalculator.Assess(new AffordabilityInput
        {
            FixedGrossSalary = 30000m,
            PayslipDeductions = 6000m,
            DeclaredLivingExpenses = new LivingExpenses
            {
                Accommodation = 6000m, Transport = 2000m, Food = 3000m, WaterAndElectricity = 1000m
            },
            CreditBureauObligations = [new ExpenseLine("Vehicle finance", 4000m)],
            BankStatementExpenses = [new ExpenseLine("Insurance", 1000m)],
            ProposedInstalment = 5000m
        });

        Assert.Equal(0.3000m, result.DebtToIncomeRatio);
        Assert.Equal(0.2083m, result.InstalmentToNetIncomeRatio);
        Assert.Equal(0.2917m, result.DiscretionaryBufferRatio);
    }
}
