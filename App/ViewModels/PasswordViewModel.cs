using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Core.Security;

namespace SecurityProgram.App.ViewModels;

public class PasswordViewModel : ViewModelBase
{
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string NumberChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    private string _passwordInput = string.Empty;
    private int _passwordScore;
    private string _passwordLevel = "Weak";
    private double _estimatedEntropyBits;
    private string _feedbackMessage = "비밀번호를 입력하면 강도를 분석합니다.";
    private string _generatedPassword = string.Empty;

    public string PasswordInput
    {
        get => _passwordInput;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _passwordInput, normalized))
            {
                EvaluatePassword();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int PasswordScore
    {
        get => _passwordScore;
        private set => SetProperty(ref _passwordScore, value);
    }

    public string PasswordLevel
    {
        get => _passwordLevel;
        private set => SetProperty(ref _passwordLevel, value);
    }

    public string PasswordLevelKorean => ToKoreanLevel(PasswordLevel);

    public double EstimatedEntropyBits
    {
        get => _estimatedEntropyBits;
        private set => SetProperty(ref _estimatedEntropyBits, value);
    }

    public string FeedbackMessage
    {
        get => _feedbackMessage;
        private set => SetProperty(ref _feedbackMessage, value);
    }

    public string GeneratedPassword
    {
        get => _generatedPassword;
        private set => SetProperty(ref _generatedPassword, value);
    }

    public ObservableCollection<string> RuleChecklist { get; } = new();

    public ICommand GenerateSamplePasswordCommand { get; }

    public ICommand CopyCurrentPasswordCommand { get; }

    public ICommand ClearCommand { get; }

    public PasswordViewModel()
    {
        GenerateSamplePasswordCommand = new RelayCommand(_ => GenerateSamplePassword());
        CopyCurrentPasswordCommand = new RelayCommand(_ => CopyCurrentPassword(), _ => !string.IsNullOrWhiteSpace(PasswordInput));
        ClearCommand = new RelayCommand(_ => ClearAll(), _ => !string.IsNullOrEmpty(PasswordInput));

        EvaluatePassword();
    }

    private void EvaluatePassword()
    {
        PasswordScore = PasswordStrengthEvaluator.Evaluate(PasswordInput);
        PasswordLevel = PasswordStrengthEvaluator.GetLevel(PasswordScore);
        OnPropertyChanged(nameof(PasswordLevelKorean));
        EstimatedEntropyBits = EstimateEntropy(PasswordInput);

        UpdateRuleChecklist();
        UpdateFeedback();
    }

    private void UpdateRuleChecklist()
    {
        RuleChecklist.Clear();

        AddRule("길이 10자 이상", PasswordInput.Length >= 10);
        AddRule("소문자를 필수로 포함해야 합니다.", PasswordInput.Any(char.IsLower));
        AddRule("대문자를 필수로 포함해야 합니다.", PasswordInput.Any(char.IsUpper));
        AddRule("숫자를 필수로 포함해야 합니다.", PasswordInput.Any(char.IsDigit));
        AddRule("특수문자를 필수로 포함해야 합니다.", PasswordInput.Any(ch => !char.IsLetterOrDigit(ch)));
        AddRule("동일 문자 2회 이상 반복 없음", !HasRepeatedCharacters(PasswordInput, 2));
    }

    private void AddRule(string rule, bool passed)
    {
        RuleChecklist.Add($"[{(passed ? "OK" : "FAIL")}] {rule}");
    }

    private void UpdateFeedback()
    {
        if (string.IsNullOrEmpty(PasswordInput))
        {
            FeedbackMessage = "비밀번호를 입력하면 강도를 분석합니다.";
            return;
        }

        var missing = new List<string>();

        if (PasswordInput.Length < 10)
        {
            missing.Add("길이를 10자 이상으로 늘리기");
        }

        if (!PasswordInput.Any(char.IsLower))
        {
            missing.Add("소문자 추가");
        }

        if (!PasswordInput.Any(char.IsUpper))
        {
            missing.Add("대문자 추가");
        }

        if (!PasswordInput.Any(char.IsDigit))
        {
            missing.Add("숫자 추가");
        }

        if (!PasswordInput.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            missing.Add("특수문자 추가");
        }

        if (missing.Count == 0 && PasswordScore >= 80)
        {
            FeedbackMessage = "강한 비밀번호입니다. 보안이 중요한 계정에 사용하기 적합합니다.";
            return;
        }

        FeedbackMessage =
            $"현재 등급: {ToKoreanLevel(PasswordLevel)}. 개선 항목: {string.Join(", ", missing)}";
    }

    private void GenerateSamplePassword()
    {
        var generated = CreateStrongPassword(length: 16);
        GeneratedPassword = generated;
        PasswordInput = generated;
    }

    private void CopyCurrentPassword()
    {
        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            return;
        }

        Clipboard.SetText(PasswordInput);
        FeedbackMessage = "비밀번호를 클립보드에 복사했습니다.";
    }

    private void ClearAll()
    {
        PasswordInput = string.Empty;
        GeneratedPassword = string.Empty;
    }

    private static double EstimateEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        var charsetSize = 0;

        if (password.Any(char.IsLower))
        {
            charsetSize += 26;
        }

        if (password.Any(char.IsUpper))
        {
            charsetSize += 26;
        }

        if (password.Any(char.IsDigit))
        {
            charsetSize += 10;
        }

        if (password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            charsetSize += SpecialChars.Length;
        }

        if (charsetSize == 0)
        {
            return 0;
        }

        var entropy = password.Length * Math.Log2(charsetSize);
        return Math.Round(entropy, 1);
    }

    private static bool HasRepeatedCharacters(string value, int repeatThreshold)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var count = 1;

        for (var index = 1; index < value.Length; index++)
        {
            if (value[index] == value[index - 1])
            {
                count++;
                if (count >= repeatThreshold)
                {
                    return true;
                }
            }
            else
            {
                count = 1;
            }
        }

        return false;
    }

    private static string CreateStrongPassword(int length)
    {
        if (length < 12)
        {
            length = 12;
        }

        var allChars = LowerChars + UpperChars + NumberChars + SpecialChars;
        var chars = new char[length];

        chars[0] = LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)];
        chars[1] = UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)];
        chars[2] = NumberChars[RandomNumberGenerator.GetInt32(NumberChars.Length)];
        chars[3] = SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)];

        for (var index = 4; index < length; index++)
        {
            chars[index] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        Shuffle(chars);

        return new string(chars);
    }

    private static void Shuffle(char[] chars)
    {
        for (var index = chars.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (chars[index], chars[swapIndex]) = (chars[swapIndex], chars[index]);
        }
    }

    private static string ToKoreanLevel(string level)
    {
        return level switch
        {
            "Weak" => "약함",
            "Medium" => "보통",
            "Strong" => "강함",
            _ => level,
        };
    }
}
