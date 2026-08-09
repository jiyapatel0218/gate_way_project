using System.ComponentModel.DataAnnotations;

namespace SocietyGatekeeper.Application.Validation;

/// <summary>
/// Mirrors the ASP.NET Identity password policy configured in Program.cs
/// (min length 8, at least one uppercase letter, at least one digit).
/// Keep this in sync if that policy changes.
/// </summary>
public class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
    {
        ErrorMessage = "Password must be at least 8 characters long and include an uppercase letter and a digit.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password) return false;

        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsDigit);
    }
}
