using System;
using System.IO;
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
            if (SetProperty(ref _password, value))
            {
                PasswordScore = PasswordStrengthEvaluator.Evaluate(value);
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

    public bool CanEncrypt =>
        !string.IsNullOrWhiteSpace(SelectedFilePath)
        && File.Exists(SelectedFilePath)
        && !string.IsNullOrWhiteSpace(Password)
        && PasswordScore >= MinimumPasswordScore;

    public ICommand BrowseFileCommand { get; }

    public ICommand EncryptCommand { get; }

    public ICommand DecryptCommand { get; }

    public EncryptionViewModel()
    {
        BrowseFileCommand = new RelayCommand(_ => BrowseFile());
        EncryptCommand = new RelayCommand(_ => Encrypt(), _ => CanEncrypt);
        DecryptCommand = new RelayCommand(_ => Decrypt(), _ => CanEncrypt);
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
        if (!CanEncrypt)
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
        CommandManager.InvalidateRequerySuggested();
    }
}
