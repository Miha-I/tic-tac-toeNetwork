using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TicTacToeNetwork
{
    class Game
    {
        public ObservableCollection<Cell> Cells { get; private set; }
        public Command<Cell> CellClickCommand { get; private set; }
        public Command<String> MenuClick { get; private set; }
        public double MaxHeight { get; private set; }

        bool isRunPlayer;
        bool isGame;
        CellTypes cellTypePlayer;
        MainWindow Owner;
        Network network;
        IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
        MenuItem MenuItemDisconnect;

        const string connection = "connect";
        const string connectedNO = "NO";
        const string connectedOK = "OK";
        const int PORT = 12790;

        public Game(MainWindow owner)
        {
            Cells = new ObservableCollection<Cell>();
            MenuClick = new Command<String>(OnClickMenu);
            CellClickCommand = new Command<Cell>(PlayerClickedCell);

            for (int i = 0; i < 9; i++)
                Cells.Add(new Cell() { Number = i, CellType = CellTypes.empty });

            cellTypePlayer = CellTypes.x;
            network = new Network();
            Owner = owner;

            network.ConnectEvent += OnConnected;
            network.ReceivedEvent += OnReceived;
            network.MessageEvent += MessageError;
            network.DisconnectEvent += MessageDisconnect;

            MenuItemDisconnect = new MenuItem();
            MenuItemDisconnect.Command = MenuClick;

            MaxHeight = SystemParameters.PrimaryScreenHeight - 100;
        }

        // Новая игра
        internal void NewGame()
        {
            ResetGame();
            isRunPlayer = false;
            isGame = false;
        }

        // Клик по подменю Сеть
        private void OnClickMenu(String parameter)
        {
            switch(parameter)
            {
                // Старт сервера
                case "ClickStartServer":
                    NewGame();
                    StartServer();
                    Owner.menuClient.IsEnabled = false;
                    Owner.menuServer.IsEnabled = false;
                    Owner.menuNetwork.Items.Add(MenuItemDisconnect);
                    MenuItemDisconnect.Header = "Остановить сервер";
                    MenuItemDisconnect.CommandParameter = "ClickStopServer";
                    break;

                // Старт клиента
                case "ClickStartClient":
                    ConnectionHost dlg = new ConnectionHost();
                    dlg.Owner = Owner;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dlg.IpAdress = "127.0.0.1";
                    dlg.ShowDialog();
                    if (dlg.DialogResult == true)
                    {
                        NewGame();
                        ConnecTotServer(dlg.IpAdress);
                        Owner.menuServer.IsEnabled = false;
                        Owner.menuClient.IsEnabled = false;
                        Owner.menuNetwork.Items.Add(MenuItemDisconnect);
                        MenuItemDisconnect.Header = "Отключиться";
                        MenuItemDisconnect.CommandParameter = "ClickDisconnect";
                    }
                    break;

                // Остановка сервера
                case "ClickStopServer":
                    network.StopServer();
                    Owner.Title = "Крестики нолики";
                    Owner.menuServer.IsEnabled = true;
                    Owner.menuClient.IsEnabled = true;
                    Owner.menuNetwork.Items.Remove(MenuItemDisconnect);
                    break;

                // Отключиться
                case "ClickDisconnect":
                    Disconnect();
                    break;
            }
        }

        // Старт сервера
        private void StartServer()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            //IPHostEntry ipHost = Dns.GetHostEntry("localhost"); // Проверка на локальном компьютере
            foreach (IPAddress addr in ipHost.AddressList)
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = addr;
                    break;
                }

            Owner.Title += " IP - " + ipAddr;
            network.StartServer(ipAddr.ToString(), PORT);
        }

        // Подключение к серверу
        private void ConnecTotServer(string ip) => network.Connection(ip, PORT);

        // Получение данных
        private void OnReceived(string data)
        {
            try
            {
                if (!isGame)
                {
                    // Если поступил зарос предложения игры
                    if (data.IndexOf(connection) != -1)
                    {
                        string addres = network.GetEndPointClient().ToString();
                        addres = addres.Substring(0, addres.IndexOf(":"));
                        string str = $"Игрок с IP адресом: {addres} хочет подключится для игры\nОсуществить подключение?";
                        if (Owner.MessageShow(str, "Подключение"))
                        {
                            MenuItemDisconnect.Dispatcher.Invoke(() => { MenuItemDisconnect.Header = "Отключиться"; });
                            network.Send(connectedOK);
                            network.StopServer();
                            network.Receive();
                            isGame = true;
                            Owner.tbPlayerMove.Dispatcher.Invoke(() => { Owner.tbPlayerMove.Text = "Ход противника"; });
                        }
                        else
                        {
                            network.Send(connectedNO);
                            network.StartListen();
                            network.Disconnect();
                        }
                    }

                    // Если поступил ответ согласия игры по сети
                    else if (data.IndexOf(connectedOK) != -1)
                    {
                        isRunPlayer = true;
                        isGame = true;
                        Owner.tbPlayerMove.Dispatcher.Invoke(() => { Owner.tbPlayerMove.Text = "Ваш ход"; });
                    }

                    // Если игрок отказался играть по сети
                    else if (data.IndexOf(connectedNO) != -1)
                    {
                        MessageBox.Show("Игрок отказался с Вами играть", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                        Disconnect();
                    }
                }
                else
                {
                    if (Int32.TryParse(data, out int cell))
                        RunEnemy(cell);
                    else
                        MessageBox.Show("Не верный форматстроки: " + data, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch(Exception ex)
            {
                MessageError(ex.Message);
            }
        }

        // Подклён к серверу
        private void OnConnected()
        {
            try
            {
                // Запрос предложения для игры по сети
                network.Send(connection);
                network.Receive();
            }
            catch (Exception ex)
            {
                MessageError(ex.Message);
            }
        }

        // Ход игрока
        private void PlayerClickedCell(Cell cell)
        {
            try
            {
                if (cell.CellType == CellTypes.empty && isRunPlayer == true)
                {
                    isRunPlayer = false;
                    Owner.tbPlayerMove.Dispatcher.Invoke(() => { Owner.tbPlayerMove.Text = "Ход противника"; });
                    Cells[cell.Number].CellType = cellTypePlayer;
                    network.Send(cell.Number.ToString());
                    network.Receive();
                    if (IsWinner(cellTypePlayer))
                    {
                        Owner.tbPlayerWin.Dispatcher.Invoke(() => { Owner.tbPlayerWin.Text = (Int32.Parse(Owner.tbPlayerWin.Text) + 1).ToString(); });
                        if (Owner.MessageShow("Вы выиграли\nИграем ещё?", "Конец игры"))
                            ResetGame();
                        else
                            Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageError(ex.Message);
            }
        }

        // Ход противника
        private void RunEnemy(int cell)
        {
            if (Cells[cell].CellType == CellTypes.empty && isRunPlayer == false)
            {
                Cells[cell].CellType = GetCellTypeEnemy(cellTypePlayer);
                isRunPlayer = true;
                Owner.tbPlayerMove.Dispatcher.Invoke(() => { Owner.tbPlayerMove.Text = "Ваш ход"; });
                if (IsWinner(GetCellTypeEnemy(cellTypePlayer)))
                {
                    Owner.tbEnemyWin.Dispatcher.Invoke(() => { Owner.tbEnemyWin.Text = (Int32.Parse(Owner.tbEnemyWin.Text) + 1).ToString(); });
                    if (Owner.MessageShow("Противник выиграл\nИграем ещё?", "Конец игры"))
                        ResetGame();                        
                    else
                        Disconnect();
                }
            }
        }

        // Проверка есть ли победитель
        private bool IsWinner(CellTypes cellType)
        {
            if (Cells[0].CellType == cellType && Cells[1].CellType == cellType && Cells[2].CellType == cellType ||
                 Cells[3].CellType == cellType && Cells[4].CellType == cellType && Cells[5].CellType == cellType ||
                  Cells[6].CellType == cellType && Cells[7].CellType == cellType && Cells[8].CellType == cellType ||
                   Cells[0].CellType == cellType && Cells[3].CellType == cellType && Cells[6].CellType == cellType ||
                    Cells[1].CellType == cellType && Cells[4].CellType == cellType && Cells[7].CellType == cellType ||
                     Cells[2].CellType == cellType && Cells[5].CellType == cellType && Cells[8].CellType == cellType ||
                      Cells[0].CellType == cellType && Cells[4].CellType == cellType && Cells[8].CellType == cellType ||
                       Cells[2].CellType == cellType && Cells[4].CellType == cellType && Cells[6].CellType == cellType)
                return true;
            return false;
        }

        // Получение фигуры противника
        private CellTypes GetCellTypeEnemy(CellTypes cellTypePlayer) => cellTypePlayer == CellTypes.o ? CellTypes.x : CellTypes.o;

        // Сброс Игры
        private void ResetGame()
        {
            foreach (var item in Cells)
                item.CellType = CellTypes.empty;
        }

        // Удаленный узел отключился
        private void MessageDisconnect()
        {
            MessageBox.Show("Противник отключился", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            Disconnect();
        }

        // Ошибка сети
        private void MessageError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            Disconnect();
        }

        // Отключение
        private void Disconnect()
        {
            isRunPlayer = false;
            Owner.tbPlayerMove.Dispatcher.Invoke(() => { Owner.tbPlayerMove.Text = ""; });
            network.Disconnect();
            Owner.menuServer.Dispatcher.Invoke(() => { Owner.menuServer.IsEnabled = true; });
            Owner.menuClient.Dispatcher.Invoke(() => { Owner.menuClient.IsEnabled = true; });
            Owner.menuNetwork.Dispatcher.Invoke(() => { Owner.menuNetwork.Items.Remove(MenuItemDisconnect); });
        }
    }
}
