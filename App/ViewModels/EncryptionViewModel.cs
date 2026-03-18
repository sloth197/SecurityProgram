using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Core.Encryption;
using SecurityProgram.App.Core.Security;

namespace SecurityProgram.App.ViewModels;

public class EncryptionViewModel : ViewModelBase
{
    private const int MinimumPasswordScore = 60;

    private readonly AesFileCryptoService _cryptoService = new();

    private string _selectedFilePath = string.Empty;
    private string _statusMessage = "Choose a file to start.";
    private string _password = string.Empty;
    private int _passwordScore;
    private string _passwordLevel = "Weak";
    private string _passwordStatusMessage = "Enter a password to evaluate strength.";
    private string _inputHint = "Step 1: Select a file. Step 2: Enter a password.";

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                UpdateCommandState();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string Password
    {
        get => _password;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _password, normalized))
            {
                PasswordScore = PasswordStrengthEvaluator.Evaluate(normalized);
                PasswordLevel = PasswordStrengthEvaluator.GetLevel(PasswordScore);
                UpdateCommandState();
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

    public string PasswordStatusMessage
    {
        get => _passwordStatusMessage;
        private set => SetProperty(ref _passwordStatusMessage, value);
    }

    public string PasswordLevelDisplay => $"Strength: {PasswordLevel} ({PasswordScore}/100)";

    public bool HasPassword => !string.IsNullOrWhiteSpace(Password);

    public bool ShowWeakWarning => HasPassword && PasswordScore < MinimumPasswordScore;

    public string InputHint
    {
        get => _inputHint;
        private set => SetProperty(ref _inputHint, value);
    }

    public bool CanEncrypt =>
        !string.IsNullOrWhiteSpace(SelectedFilePath)
        && File.Exists(SelectedFilePath)
        && HasPassword
        && PasswordScore >= MinimumPasswordScore;

    public bool CanDecrypt =>
        !string.IsNullOrWhiteSpace(SelectedFilePath)
        && File.Exists(SelectedFilePath)
        && HasPassword;

    public ICommand BrowseFileCommand { get; }

    public ICommand EncryptCommand { get; }

    public ICommand DecryptCommand { get; }

    public EncryptionViewModel()
    {
        BrowseFileCommand = new RelayCommand(_ => BrowseFile());
        EncryptCommand = new RelayCommand(_ => Encrypt(), _ => CanEncrypt);
        DecryptCommand = new RelayCommand(_ => Decrypt(), _ => CanDecrypt);
        UpdateCommandState();
    }

    private void BrowseFile()
    {
        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            StatusMessage = "File selected.";
        }
    }

    private void Encrypt()
    {
        if (!CanEncrypt)
        {
            StatusMessage = "File or password is not ready.";
            return;
        }

        try
        {
            var output = _cryptoService.EncryptFile(SelectedFilePath, Password);
            StatusMessage = $"Encrypted: {output}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Encryption failed: {ex.Message}";
        }
    }

    private void Decrypt()
    {
        if (!CanDecrypt)
        {
            StatusMessage = "File or password is not ready.";
            return;
        }

        try
        {
            var output = _cryptoService.DecryptFile(SelectedFilePath, Password);
            StatusMessage = $"Decrypted: {output}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Decryption failed: {ex.Message}";
        }
    }

    private void UpdateCommandState()
    {
        OnPropertyChanged(nameof(CanEncrypt));
        OnPropertyChanged(nameof(CanDecrypt));
        OnPropertyChanged(nameof(PasswordLevelDisplay));
        OnPropertyChanged(nameof(HasPassword));
        OnPropertyChanged(nameof(ShowWeakWarning));

        PasswordStatusMessage = BuildPasswordStatusMessage();
        InputHint = BuildInputHint();

        CommandManager.InvalidateRequerySuggested();
    }

    private string BuildInputHint()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            return "Select a file to enable actions.";
        }

        if (!File.Exists(SelectedFilePath))
        {
            return "Selected file cannot be found. Please browse again.";
        }

        if (!HasPassword)
        {
            return "Enter a password to continue.";
        }

        if (!CanEncrypt)
        {
            return $"Encrypt requires score {MinimumPasswordScore}+ (current: {PasswordScore}). Decrypt is available.";
        }

        return "Ready: Encrypt and Decrypt are available.";
    }

    private string BuildPasswordStatusMessage()
    {
        if (!HasPassword)
        {
            return "Enter a password to evaluate strength.";
        }

        if (!ShowWeakWarning)
        {
            return "Password strength is sufficient for encryption.";
        }

        var recommendations = new List<string>();

        if (Password.Length < 12)
        {
            recommendations.Add("12+ characters");
        }

        if (!Password.Any(char.IsUpper))
        {
            recommendations.Add("uppercase");
        }

        if (!Password.Any(char.IsLower))
        {
            recommendations.Add("lowercase");
        }

        if (!Password.Any(char.IsDigit))
        {
            recommendations.Add("numbers");
        }

        if (!Password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            recommendations.Add("special symbols");
        }

        return recommendations.Count == 0
            ? "Weak password. Increase complexity to improve protection."
            : $"Weak password. Add: {string.Join(", ", recommendations)}.";
    }
}
