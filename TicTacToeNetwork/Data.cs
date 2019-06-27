using System;
using System.ComponentModel;
using System.Windows.Input;

namespace TicTacToeNetwork
{
    public delegate void ConnectEventHandler();
    public delegate void SocketEventHandler(string data);
    public delegate bool MessageEventHandler(string message, string title);
  

    // Клетка
    class Cell : INotifyPropertyChanged
    {
        public int Number { get; set; }
        private CellTypes cellType;
        public CellTypes CellType
        {
            get { return cellType; }
            set
            {
                cellType = value;
                NotifyPropertyChanged("CellType");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Типы клеток
    enum CellTypes
    {
        empty,
        x,
        o
    }

    // Реализация команд
    class Command<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public Action<T> Action { get; set; }

        public Command(Action<T> action)
        {
            Action = action;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (Action != null && parameter is T)
                Action((T)parameter);
        }
    }
}
