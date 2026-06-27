namespace VeridocX.Server.Domain.SaId;

public static class SaIdValidator
{
    public static SaIdValidationResult Validate(string? rawInput)
    {
        var input = (rawInput ?? string.Empty).Trim();
        var checks = new List<ValidationCheck>();

        var wellFormed = input.Length == 13 && input.All(char.IsDigit);
        checks.Add(new ValidationCheck(
            "format",
            wellFormed,
            wellFormed ? "13 numeric digits" : $"expected 13 digits, got {input.Length} character(s)"));

        if (!wellFormed)
            return new SaIdValidationResult { Input = input, IsValid = false, Checks = checks };

        var dob = TryParseDateOfBirth(input[..6]);
        checks.Add(new ValidationCheck(
            "date_of_birth",
            dob is not null,
            dob is not null ? $"valid date {dob:yyyy-MM-dd}" : $"invalid date segment '{input[..6]}'"));

        var genderSequence = int.Parse(input.Substring(6, 4));
        var gender = genderSequence < 5000 ? Gender.Female : Gender.Male;
        checks.Add(new ValidationCheck(
            "gender",
            true,
            $"sequence {genderSequence:0000} -> {gender}"));

        var citizenshipDigit = input[10] - '0';
        var citizenship = citizenshipDigit switch
        {
            0 => Citizenship.Citizen,
            1 => Citizenship.PermanentResident,
            _ => Citizenship.Unknown
        };
        checks.Add(new ValidationCheck(
            "citizenship",
            citizenship != Citizenship.Unknown,
            $"digit '{citizenshipDigit}' -> {citizenship}"));

        var checksumValid = PassesLuhn(input);
        checks.Add(new ValidationCheck(
            "checksum",
            checksumValid,
            checksumValid ? "Luhn check digit valid" : "Luhn check digit mismatch (typo or tampering)"));

        return new SaIdValidationResult
        {
            Input = input,
            IsValid = checks.All(c => c.Passed),
            Checks = checks,
            DateOfBirth = dob,
            Gender = gender,
            Citizenship = citizenship == Citizenship.Unknown ? null : citizenship
        };
    }

    private static DateOnly? TryParseDateOfBirth(string yymmdd)
    {
        var yy = int.Parse(yymmdd[..2]);
        var mm = int.Parse(yymmdd.Substring(2, 2));
        var dd = int.Parse(yymmdd.Substring(4, 2));

        var currentYy = DateTime.UtcNow.Year % 100;
        var year = yy <= currentYy ? 2000 + yy : 1900 + yy;

        if (mm is < 1 or > 12) return null;
        if (dd < 1 || dd > DateTime.DaysInMonth(year, mm)) return null;

        return new DateOnly(year, mm, dd);
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleNext = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleNext)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            doubleNext = !doubleNext;
        }
        return sum % 10 == 0;
    }
}
