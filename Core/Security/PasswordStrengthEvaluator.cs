using System.Text.RegularExpressions;

namespace SecurityProgram.App.Core.Security
{
    public static class PasswordStrengthEvaluator
    {
        public static int Evaluate(string password)
        {
            if(string.IsNullOrEmpty(password))
                return 0;
            int score = 0;

            if(password.Length >= 8) 
                score += 20;
            else if(password.Length >= 12)
                score += 20;
            else if(Regex.IsMatch(password, "[a-z]"))
                score += 15;
            else if(Regex.IsMatch(password, "[A-Z]"))
                score += 15;
            else if(Regex.IsMatch(password, "[0-9]"))
                score += 15;
            else if(Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                score += 15;
            return score > 100 ? 100 :score;
        }
        public static string GetLevel(int score)
        {
            if(score < 40)
                return "Weak";
            else if(score < 70)
                return "Medium";
            return "Strong";
        }
    }
}