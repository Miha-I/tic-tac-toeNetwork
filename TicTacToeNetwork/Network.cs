using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TicTacToeNetwork
{
    class Network: IDisposable
    {        
        public event ConnectEventHandler DisconnectEvent;      
        public event ConnectEventHandler ConnectEvent;
        public event SocketEventHandler ReceivedEvent;
        public event SocketEventHandler MessageEvent;
        IPEndPoint endPoint;
        Socket listenSocket;
        Socket workSocket;

        public bool IsConnected = false;
        byte[] buffer = new byte[64];

        // Старт сервера
        public void StartServer(string addres, int port)
        {
            if (listenSocket != null)
            {
                listenSocket?.Close();
                listenSocket = null;
            }
            IPAddress ipAddr = IPAddress.Parse(addres);
            endPoint = new IPEndPoint(ipAddr, port);
            listenSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(endPoint);
            listenSocket.Listen(10);
            StartListen();
        }

        // Прием входящих подключений
        public void StartListen() => listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenSocket);

        // Приём и обработка подключений
        private void AcceptCallback(IAsyncResult state)
        {
            Socket socket = (Socket)state.AsyncState;
            try
            {
                workSocket = socket.EndAccept(state);
                IsConnected = true;
                Receive();             
            }
            catch (ObjectDisposedException ex)
            {
                listenSocket = null;
            }
            catch (Exception ex)
            {
                if (MessageEvent != null)
                    MessageEvent("Ошибка сервера: " + ex.Message);
                listenSocket?.Close();
                listenSocket = null;
            }
        }

        // Остановка сервера
        public void StopServer() => listenSocket?.Close();

        // Отключение
        public void Disconnect()
        {
            workSocket?.Close();
            workSocket = null;
            IsConnected = false;
        }

        // Подключение к удалённому узлу
        public void Connection(string addres, int port)
        {
            if (workSocket != null)
                return;

            IPAddress ipAddr = IPAddress.Parse(addres);
            Socket socket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(ipAddr, port, (state) =>
            {
                try
                {
                    socket.EndConnect(state);
                    if (socket.Connected)
                    {
                        workSocket = socket;
                        IsConnected = true;
                        if (ConnectEvent != null)
                            ConnectEvent();
                    }
                }
                catch (SocketException ex)
                {
                    if (MessageEvent != null)
                        MessageEvent("Ошибка клиента: " + ex.Message);
                }
                catch (Exception ex)
                {
                    if (MessageEvent != null)
                        MessageEvent("Ошибка: " + ex.Message);
                }
            }, null);
        }

        // Отправка данных
        public void Send(string data)
        {
            if (!IsConnected)
                return;
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            workSocket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, (state) =>
            {
                try
                {
                    if (workSocket == null)
                        return;
                    int bytesTransferred = workSocket.EndSend(state);
                    if (bytesTransferred == 0)
                        workSocket.Close();
                }
                // Связь прервана
                catch (SocketException ex)
                {
                    if (DisconnectEvent != null)
                        DisconnectEvent();
                }
                catch (Exception ex)
                {
                    if (MessageEvent != null)
                        MessageEvent("Ошибка: " + ex.Message);
                }

            }, null);

        }

        // Приём данных
        public void Receive()
        {
            if (!IsConnected || workSocket == null)
                return;

            workSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, (state) =>
            {
                try
                {
                    if (workSocket == null)
                        return;
                    int bytesTransferred = workSocket.EndReceive(state);
                    if (bytesTransferred > 0)
                        if (ReceivedEvent != null)
                            ReceivedEvent(Encoding.UTF8.GetString(buffer, 0, bytesTransferred));
                }
                // Связь прервана
                catch (SocketException ex)
                {
                    if (DisconnectEvent != null)
                        DisconnectEvent();
                }
                catch (Exception ex)
                {
                    if (MessageEvent != null)
                        MessageEvent("Ошибка: " + ex.Message);
                }
            }, null);
        }

        // Освобождение ресурсов
        public void Dispose()
        {
            listenSocket?.Close();
            workSocket?.Close();
            listenSocket = null;
            workSocket = null;          
        }

        // Адресс клиента
        public EndPoint GetEndPointClient()
        {
            return workSocket.RemoteEndPoint;
        }
    }
}
