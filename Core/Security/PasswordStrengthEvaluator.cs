using System.Text.RegularExpressions;

namespace SecurityProgram.App.Core.Security;

public static partial class PasswordStrengthEvaluator
{
    public static int Evaluate(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        var score = 0;

        if (password.Length >= 8)
        {
            score += 25;
        }

        if (password.Length >= 12)
        {
            score += 10;
        }

        if (LowerRegex().IsMatch(password))
        {
            score += 15;
        }

        if (UpperRegex().IsMatch(password))
        {
            score += 15;
        }

        if (NumberRegex().IsMatch(password))
        {
            score += 15;
        }

        if (SpecialRegex().IsMatch(password))
        {
            score += 20;
        }

        return score > 100 ? 100 : score;
    }

    public static string GetLevel(int score)
    {
        if (score < 40)
        {
            return "Weak";
        }

        if (score < 70)
        {
            return "Medium";
        }

        return "Strong";
    }

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowerRegex();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex NumberRegex();

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex SpecialRegex();
}
