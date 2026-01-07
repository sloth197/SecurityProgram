using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using SecurityProgram.App.Commands;

namespace SecurityProgram.App.ViewModels
{
    public class EncryptionViewModel : INotifyPropertyChanged
    {
        //Add Field 
        private string _selectedFilePath;
        private string _statusMessage;
        private string _password;
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
        public string Password{
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
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
        }
            StatusMessage = "구현 아직 못함 ㅜㅜ";
    
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
            StatusMessage = "얘도 아직 구현 못함 ㅜㅜ"
        }
        public event PropertyChangedEventHandler  PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(rhis, new PropertyChangedEventArgs(name));
    }
}
