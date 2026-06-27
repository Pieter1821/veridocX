using VeridocX.Server.Domain.Ocr;
using VeridocX.Server.Domain.Payslip;
using Xunit;

namespace VeridocX.Tests;

public class PayslipParserTests
{
    [Theory]
    [InlineData("R 25 000.00", 25000.00)]
    [InlineData("25 000,00", 25000.00)]
    [InlineData("1,234,567.89", 1234567.89)]
    [InlineData("1.234.567,89", 1234567.89)]
    [InlineData("21250", 21250)]
    [InlineData("R3 500.00", 3500.00)]
    [InlineData("12,50", 12.50)]
    public void ParseAmount_handles_sa_money_formats(string raw, decimal expected)
    {
        Assert.Equal(expected, PayslipParser.ParseAmount(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no digits here")]
    public void ParseAmount_returns_null_when_no_number(string raw)
    {
        Assert.Null(PayslipParser.ParseAmount(raw));
    }

    private const string SamplePayslip = """
        ACME Trading (Pty) Ltd
        Payslip for: John Doe
        ID Number: 8001015009087
        Pay Period: March 2026
        Earnings
        Basic Salary           R 25 000.00
        Gross Pay              R 25 000.00
        Deductions
        PAYE                     R 3 500.00
        UIF                          R 250.00
        Total Deductions     R 3 750.00
        Nett Pay                R 21 250.00
        """;

    [Fact]
    public void Parse_extracts_core_figures()
    {
        var result = PayslipParser.Parse(SamplePayslip);

        Assert.Equal(25000.00m, result.GrossEarnings);
        Assert.Equal(21250.00m, result.NetPay);
        Assert.Equal(3500.00m, result.Paye);
        Assert.Equal(250.00m, result.Uif);
    }

    [Fact]
    public void Parse_finds_the_id_number()
    {
        var result = PayslipParser.Parse(SamplePayslip);
        Assert.Equal("8001015009087", result.IdNumber);
    }

    [Fact]
    public void Parse_returns_nulls_for_missing_fields()
    {
        var result = PayslipParser.Parse("Just some random text with no figures");

        Assert.Null(result.GrossEarnings);
        Assert.Null(result.NetPay);
        Assert.Null(result.Paye);
        Assert.Null(result.Uif);
    }

    private static OcrTable Table(params string[][] rows) =>
        new(rows.Select(r => (IReadOnlyList<string>)r).ToList());

    private static OcrDocument TabularPayslip()
    {
        var summary = Table(
            ["SUMMARY", "AMOUNT (R)"],
            ["Total Earnings", "20 500.00"],
            ["Total Deductions", "4 726.18"],
            ["NET PAY", "15 773.82"]);

        var deductions = Table(
            ["DESCRIPTION", "AMOUNT (R)"],
            ["PAYE (Income Tax)", "2 987.52"],
            ["UIF Employee", "177.12"],
            ["Pension Fund", "2 050.00"],
            ["Medical Aid", "1 250.00"]);

        var yearToDate = Table(
            ["YEAR TO DATE", "AMOUNT (R)"],
            ["Total Earnings", "112 750.00"],
            ["PAYE", "17 203.75"],
            ["UIF Employee", "1 062.72"]);

        var employer = Table(
            ["EMPLOYER CONTRIBUTIONS (YTD)", "AMOUNT (R)"],
            ["UIF Employer", "1 062.72"]);

        const string text = "PAYSLIP\nID Number 900101 1234 085\nEmployee Name Thabo John Mokoena";

        return new OcrDocument(text, [summary, deductions, yearToDate, employer]);
    }

    [Fact]
    public void Parse_reads_current_period_figures_from_tables()
    {
        var result = PayslipParser.Parse(TabularPayslip());

        Assert.Equal(20500.00m, result.GrossEarnings);
        Assert.Equal(15773.82m, result.NetPay);
        Assert.Equal(2987.52m, result.Paye);
        Assert.Equal(177.12m, result.Uif);
    }

    [Fact]
    public void Parse_ignores_year_to_date_totals()
    {
        var result = PayslipParser.Parse(TabularPayslip());

        Assert.NotEqual(112750.00m, result.GrossEarnings);
        Assert.NotEqual(17203.75m, result.Paye);
        Assert.NotEqual(1062.72m, result.Uif);
    }

    [Fact]
    public void Parse_finds_id_number_from_table_document_text()
    {
        var result = PayslipParser.Parse(TabularPayslip());
        Assert.Equal("9001011234085", result.IdNumber);
    }

    private static OcrDocument SideBySidePayslip()
    {
        var table = Table(
            ["EARNINGS", "Amount", "DEDUCTIONS", "Amount"],
            ["Basic Salary", "35,000.00", "PAYE", "7,250.00"],
            ["Travel Allowance", "3,000.00", "UIF", "177.12"],
            ["Cell Allowance", "500.00", "Provident Fund", "3,500.00"],
            ["Overtime", "2,500.00", "Medical Aid", "2,250.00"],
            ["Performance Bonus", "1,000.00"],
            ["GROSS PAY", "42,000.00", "TOTAL DEDUCTIONS", "13,177.12"],
            ["NET PAY", "28,822.88"]);

        const string text = "ABC TECHNOLOGIES (PTY) LTD\nID Number 9001015000088 Tax No. 9999999999";

        return new OcrDocument(text, [table]);
    }

    private static OcrDocument MergedWideTablePayslip()
    {
        var summary = Table(
            ["SUMMARY", "AMOUNT (R)"],
            ["Total Earnings", "20 500.00"],
            ["NET PAY", "15 773.82"]);

        var wide = Table(
            ["EARNINGS", "HOURS / UNITS", "RATE (R)", "AMOUNT (R)", "DEDUCTIONS", "AMOUNT (R)", "YEAR TO DATE", "AMOUNT (R)"],
            ["Basic Salary", "1.00", "18 000.00", "18 000.00", "PAYE (Income Tax)", "2 987.52", "Total Earnings", "112 750.00"],
            ["Overtime - Weekdays", "10.00", "150.00", "1 500.00", "UIF Employee", "177.12", "PAYE", "17 203.75"]);

        const string text = "PAYSLIP\nID Number 900101 1234 085";

        return new OcrDocument(text, [summary, wide]);
    }

    [Fact]
    public void Parse_reads_deductions_from_merged_wide_table_and_ignores_ytd_columns()
    {
        var result = PayslipParser.Parse(MergedWideTablePayslip());

        Assert.Equal(20500.00m, result.GrossEarnings);
        Assert.Equal(15773.82m, result.NetPay);
        Assert.Equal(2987.52m, result.Paye);
        Assert.Equal(177.12m, result.Uif);
        Assert.NotEqual(17203.75m, result.Paye);
    }

    [Fact]
    public void Parse_reads_side_by_side_earnings_and_deductions()
    {
        var result = PayslipParser.Parse(SideBySidePayslip());

        Assert.Equal(42000.00m, result.GrossEarnings);
        Assert.Equal(28822.88m, result.NetPay);
        Assert.Equal(7250.00m, result.Paye);
        Assert.Equal(177.12m, result.Uif);
        Assert.Equal("9001015000088", result.IdNumber);
    }
}
