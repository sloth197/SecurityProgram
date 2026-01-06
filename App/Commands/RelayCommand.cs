using System;
using System.Windows.Input;

namespace SecurityProgram.App.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool canExecute(object parameter)       
            => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter)       
            => _execute(parameter);
        public event EventHandler canExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }  
    }
}
