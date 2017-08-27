using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WDriveConnection
{
    public class UDPConnection : GenericCommunication, IWDriveConnection
    {
        private readonly WDriveInterpreter _interpreter;
        private readonly IPAddress _ipAdress;

        private readonly int _udpPort;
        private bool _searchClients = true;
        private UdpClient _udpSocket;
        private Thread _recieverThread;

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="newData"> </param>
        /// <param name="udpPort"> </param>
        /// <param name="isConnected"> </param>
        public UDPConnection(IPAddress ipadress, NewDataFromSerialArrived newData, int udpPort,
            AutoResetEvent isConnected)
            : base(newData)
        {
            _interpreter = new WDriveInterpreter(newData);
            _udpPort = udpPort;
            _ipAdress = ipadress;
        }

        public void Open()
        {
            _recieverThread = new Thread(StartServer);
            _recieverThread.Name = "WDrive Thread: " + _udpPort;
            _recieverThread.Start();
        }

        /// <summary>
        ///     Close the Seriallistener, Aborts all threads
        /// </summary>
        public void Close()
        {
            _searchClients = false;
            _recieverThread.Abort();
            _udpSocket.Close();
        }

        /// <summary>
        ///     Write order to serial
        ///     waits until reader has recieved answer ticket
        /// </summary>
        /// <returns></returns>
        public bool Write(string str, bool text = false)
        {
            return false;
        }

        public void StartServer()
        {
            _udpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, _udpPort));
            Console.WriteLine("UDP - Begin Recieve on Port: " + _udpPort + " on: " + _ipAdress);

            while (_searchClients)
            {
                var localHostIPEnd = new IPEndPoint(IPAddress.Any, _udpPort);
                Byte[] receiveBytes = _udpSocket.Receive(ref localHostIPEnd);
                _interpreter.InterpretBytes(receiveBytes);
            }
        }
    }
}