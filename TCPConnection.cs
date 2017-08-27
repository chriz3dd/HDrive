using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
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
        }

        public void Open()
        {
            _searchClientsThread = new Thread(TCPServer);
            _searchClientsThread.Name = "HDrive Thread: " + _tcpPort;
            _searchClientsThread.Start();
        }


        /// <summary>
        ///     Close the Seriallistener, Aborts all threads
        /// </summary>
        public void Close()
        {
            if (_listener != null && _listener.Connected)
            {
                _listener.Shutdown(SocketShutdown.Both);
            }
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
        public bool Write(string str, bool text = false)
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
                Console.WriteLine("TCP Writer failer: " + e);
                return false;
            }
            return true;
        }

        private void TCPServer()
        {
            Console.WriteLine("TCP - Server started waiting for clients:" + _ipAdress + " on Port: " + _tcpPort);
            //generate Stateobject for buffer and socketinformation for async recieve
            StateObject state = new StateObject();
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           
            // Disable the Nagle Algorithm for this tcp socket.
            _listener.NoDelay = true;

            try
            {
                //open socket and cancle if no connection could be made after 5 sekonds
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

                Console.WriteLine("TCP - Serverconnected on:" + _ipAdress + " on Port: " + _tcpPort);
                _isConnected.Set();
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
               
            int recv = handler.EndReceive(ar);

            String hdriveTicket = Encoding.ASCII.GetString(state.Buffer, 0, recv);
            ticket += hdriveTicket;

                while (ticket.Length > 200)
                {
                    int start = ticket.IndexOf("<");
                    int end = ticket.IndexOf(">");

                    if (end < start)
                    {
                        ticket = ticket.Substring(end + 1, ticket.Length - end - 1);
                    }

                    else if ((end - start) > 300) //ticket to big, missing clos tag '>'?
                    {
                        ticket = "";
                    }
                    else if ((end - start) > 40)
                    {
                        String mine = ticket.Substring(start, end + 1);
                        _newDataEvent(mine, new byte[]{});
                        ticket = ticket.Remove(start, end + 1);
                    }
                    else
                    {
                        ticket += hdriveTicket;
                    }
                }
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveData), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}