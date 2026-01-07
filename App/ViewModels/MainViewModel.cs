using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Views;

namespace SecurityProgram.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentViewModel;

        public object _currentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged()
            }
        }
        public ICommand ShowEncryptionCommand {get;}
        public ICommand ShowEventLogCommand {get;}
        public ICommand ShowPasswordCommand {get;}
        public ICommand SowNetworkCommand {get;}
        public ICommand ShowReportCommand {get;}

        public MainViewModel()
        {
            CurrentViewModel = new EncryptionViewModel();
            ShowEncryptionCommand = new RelayCommand(_ => CurrentViewModel = new EncryptionViewModel());
            ShowEventLogCommand = new RelayCommand(_ => CurrentViewModel = new EventLogViewModel());
            ShowPasswordCommand = new RelayCommand(_ => CurrentViewModel = new PasswordViewModel());
            ShowNetworkCommand = new RelayCommand(_ => CurrentViewModel = new NetworkViewModel());
            ShowReportCommand = new RelayCommand(_ => CurrentViewModel = new ReportViewModel());        
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEvertArgs(name));
    }
}