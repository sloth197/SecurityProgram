using System.ComponentModel;
using System.Runtime.COmpilerServices;
using System.Windows.Input;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Views;

namespace SecurityProgram.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;

        public object _currentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
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
            CurrentView = new EncryptionView();
            ShowEncryptionCommand = new RelayCommand(_ => CurrentView = new EncryptionView());
            ShowEventLogCommand = new RelayCommand(_ => CurrentView = new EventLogView());
            ShowPasswordCommand = new RelayCommand(_ => CurrentView = new PasswordView());
            ShowNetworkCommand = new RelayCommand(_ => CurrentView = new NetworkView());
            ShowReportCommand = new RelayCommand(_ => CurrentView = new ReportView());        
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEvertArgs(name));
    }
}