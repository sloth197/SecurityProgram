using System;
using System.Windows.Input;
using SecurityProgram.App.Commands;

namespace SecurityProgram.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object? _currentViewModel;

    private readonly EncryptionViewModel _encryptionViewModel = new();
    private readonly EventLogViewModel _eventLogViewModel = new();
    private readonly PasswordViewModel _passwordViewModel = new();
    private readonly NetworkViewModel _networkViewModel = new();
    private readonly ReportViewModel _reportViewModel;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public ICommand ShowEncryptionCommand { get; }

    public ICommand ShowEventLogCommand { get; }

    public ICommand ShowPasswordCommand { get; }

    public ICommand ShowNetworkCommand { get; }

    public ICommand ShowReportCommand { get; }

    public MainViewModel()
    {
        _reportViewModel = new ReportViewModel(
            _passwordViewModel,
            _networkViewModel,
            _eventLogViewModel);

        CurrentViewModel = _encryptionViewModel;

        ShowEncryptionCommand = new RelayCommand(_ => CurrentViewModel = _encryptionViewModel);
        ShowEventLogCommand = new RelayCommand(_ => CurrentViewModel = _eventLogViewModel);
        ShowPasswordCommand = new RelayCommand(_ => CurrentViewModel = _passwordViewModel);
        ShowNetworkCommand = new RelayCommand(_ => CurrentViewModel = _networkViewModel);
        ShowReportCommand = new RelayCommand(_ => CurrentViewModel = _reportViewModel);
    }
}
