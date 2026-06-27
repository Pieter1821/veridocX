using VeridocX.Server.Domain.SaId;
using Xunit;

namespace VeridocX.Tests;

public class SaIdValidatorTests
{
    private const string ValidId = "8001015009087";

    [Fact]
    public void Valid_id_passes_every_check()
    {
        var result = SaIdValidator.Validate(ValidId);

        Assert.True(result.IsValid);
        Assert.Equal(new DateOnly(1980, 1, 1), result.DateOfBirth);
        Assert.Equal(Gender.Male, result.Gender);
        Assert.Equal(Citizenship.Citizen, result.Citizenship);
    }

    [Fact]
    public void Tampered_check_digit_fails_checksum()
    {
        var result = SaIdValidator.Validate("8001015009088");

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.Name == "checksum" && !c.Passed);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("80010150090AB")]
    [InlineData("")]
    public void Malformed_input_fails_format(string input)
    {
        var result = SaIdValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.Name == "format" && !c.Passed);
    }

    [Fact]
    public void Impossible_date_fails_date_check()
    {
        var result = SaIdValidator.Validate("8013015009087");

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.Name == "date_of_birth" && !c.Passed);
    }

    [Fact]
    public void Female_sequence_is_detected()
    {
        var result = SaIdValidator.Validate("8001010000084");

        Assert.Equal(Gender.Female, result.Gender);
    }
}
