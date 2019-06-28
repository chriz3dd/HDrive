using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WDriveConnection
{

    // State object for receiving data from remote device.
    public class StateObject
    {
        public Socket WorkSocket = null;
        public const int BufferSize = 250;
        public byte[] Buffer = new byte[BufferSize];
    }

    public class TCPConnection : GenericCommunication, IWDriveConnection
    {
        private readonly IPAddress _ipAdress;
        private readonly AutoResetEvent _isConnected;
        private readonly NewDataFromSerialArrived _newDataEvent;

        private readonly int _tcpPort;
        private Socket _listener;
        private Thread _searchClientsThread;

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="newData"> </param>
        /// <param name="tcpPort"> </param>
        /// <param name="isConnected"> </param>
        public TCPConnection(IPAddress ip, NewDataFromSerialArrived newData, int tcpPort, AutoResetEvent isConnected)
            : base(newData)
        {
            _newDataEvent = newData;
            _isConnected = isConnected;
            _tcpPort = tcpPort;
            _ipAdress = ip;

            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Disable the Nagle Algorithm for this tcp socket.
            _listener.NoDelay = true;

        }

        public void Open()
        {
            _searchClientsThread = new Thread(TCPServer);
            _searchClientsThread.Name = "HDrive TCP connection thread on TCP port: " + _tcpPort;
            _searchClientsThread.Start();
        }


        /// <summary>
        ///     Close the Seriallistener, Aborts all threads
        /// </summary>
        public void Close()
        {
            if (_listener != null && _listener.Connected)
                _listener.Shutdown(SocketShutdown.Both);

            if (_listener != null)
                _listener.Close();

            _searchClientsThread.Abort();
        }


        /// <summary>
        ///     Write order to serial
        ///     waits until reader has recieved answer ticket
        /// </summary>
        /// <param name="str"> </param>
        /// <param name="text"> </param>
        /// <returns></returns>
        public bool Write(byte[] byteArray)
        {
            try
            {
                if (_listener != null && _listener.Connected)
                {
                    // Disable the Nagle Algorithm for this tcp socket.
                    _listener.NoDelay = true;
                    _listener.SendBufferSize = byteArray.Length;
                    _listener.Send(byteArray, 0, byteArray.Length, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TCP Writer error: " + e);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Write order to serial
        ///     waits until reader has recieved answer ticket
        /// </summary>
        /// <param name="str"> </param>
        /// <param name="text"> </param>
        /// <returns></returns>
        public bool Write(string str)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(str);
            try
            {
                if (_listener != null && _listener.Connected)
                {
                    // Disable the Nagle Algorithm for this tcp socket.
                    _listener.NoDelay = true;
                    _listener.SendBufferSize = byteArray.Length;
                    _listener.Send(byteArray, 0, byteArray.Length, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TCP Writer error: " + e);
                return false;
            }
            return true;
        }

        private void TCPServer()
        {
            Debug.WriteLine("TCP - starting server. Waiting for client: " + _ipAdress + ":" + _tcpPort);

            // Generate State object for buffer and socket information for async receieve
            StateObject state = new StateObject();

            try
            {
                // Open socket and cancle if no connection could be made after 5 seconds
                IAsyncResult result = _listener.BeginConnect(new IPEndPoint(_ipAdress, _tcpPort), null, null);
                bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                if (success)
                    _listener.EndConnect(result);
                else
                {
                    _listener.Close();
                    throw new SocketException(10060); // Connection timed out.
                }

                state.WorkSocket = _listener;
                _listener.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveData), state);

                // Send the caller a signal that the socket is connected
                _isConnected.Set();

                Debug.WriteLine("TCP - Serverconnected on:" + _ipAdress + " on Port: " + _tcpPort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string ticket = "";

        private void ReceiveData(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            if (!handler.Connected)
                return;
            try
            {
                int start, end;
                int recv = handler.EndReceive(ar);

                // Append recevied ticket
                ticket += Encoding.ASCII.GetString(state.Buffer, 0, recv);

                // Interpret all full tickets containing a < and a >
                while (ticket.IndexOf(">") > -1)
                {
                    start = ticket.IndexOf("<");
                    end = ticket.IndexOf(">");

                    if (end > 0 && start >= 0 && end > start)
                    {
                        String firstTicket = ticket.Substring(start, end + 1);

                        // Send this ticket to interpreter
                        _newDataEvent(firstTicket, new byte[] { });

                        // Trim remaning string
                        ticket = ticket.Remove(start, end + 1);
                    }

                    // Trim remaning string
                    else if (end > 0)
                        ticket = ticket.Remove(0, end + 1);
                }

                // Recive and wait
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveData), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}