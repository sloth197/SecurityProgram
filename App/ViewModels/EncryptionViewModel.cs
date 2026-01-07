using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Core.Encryption;
using SecurityProgram.App.Core.Security;

namespace SecurityProgram.App.ViewModels
{
    public class EncryptionViewModel : INotifyPropertyChanged
    {
        //Add Field 
        private string _selectedFilePath;
        private string _statusMessage;
        private string _password;
        private readonly AesFileCryptoService _cryptoService = new();
        private int _passwordScore;
        private string _passwordLevel;

        //Add Status
        public string _selectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                _selectedFilePath = value;
                OnPropertyChanged();
            }
        }
        public string _statusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }   
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();

                PasswordScore = PasswordStrengthEvaluator.Evalaute(value);
                PasswordLevel = PasswordStrengthEvaluateor.GetLevel(PasswordScore);
            }
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand EncryptCommand { get; }
        public ICommand DecryptCommand { get; }

        public EncryptionViewModel()
        {
            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            EncryptCommand = new RelayCommand(_ => Encrypt());
            DecryptCommand = new RelayCommand(_ => Decrypt());
            _statusMessage = "파일을 선택하세요";
        }
        private void BrowseFile()
        {
            var dialog = new OpenFileDialog();
            if(dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                StatusMessage = "파일 선택 완료";
            }
        }
        private void Encrypt()
        {
            if(string.IsNullOrEmpty(SelectedFilePath))
            {
                StatusMessage = "일단 파일을 선택하쇼";
                return;
            }
            else if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "비밀번호 입력해주쇼";
                return;
            }  
            try
            {
                _cryptoService.EncryptFile(SelectedFilePath, Password);
                StatusMessage = "파일 암호화 했슴당"
            }
            catch 
            {
                StatusMessage = "하다가 오류 걸림 ㅜ"
            }   
        }
    
        private void Decrypt()
        {
            if(string.IsNullOrEmpty(SelectedFilePath))
            {
                StatusMessage = "일단 파일을 선택하쇼";
                return;
            }
            else if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "비밀번호 입력해주쇼";
                return;
            }
            try
            {
                _CryptoService.DecryptFile(SelectedFilePath, Password);
                StatusMessage = "파일 복호화 완료 했슴당";
            }
            catch
            {
                StatusMessage = "실패함 ㅜㅜ 비밀번호 확인 좀 하쇼"
            }
        }
        public int _passwordScore
        {
            get => _passwordScore;
            set
            {
                _passwordScorre = value;
                OnPropertyChanged();
            }
        }
        public string _PasswordLevel
        {
            get => _passwordLevel;
            set
            {
                _passwordLevel = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler  PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
