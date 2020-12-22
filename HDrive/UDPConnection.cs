using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HDrive
{
    public class UdpConnection
    {
        private readonly Action<string, byte[]> _interpreter;
        private readonly IPAddress _ipAddress;
        private readonly int _udpPort;

        private UdpClient _udpSocket;
        private Thread _receiverThread;
        private bool _searchClients = true;

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="newData"> </param>
        /// <param name="udpPort"> </param>
        /// <param name="isConnected"> </param>
        public UdpConnection(IPAddress ipAddress, Action<string, byte[]> newData, int udpPort, AutoResetEvent isConnected)
        {
            //   _interpreter = new HDriveInterpreter(newData);
            _interpreter = newData;
            _udpPort = udpPort;
            _ipAddress = ipAddress;
        }

        /// <summary>
        /// Opens the UDP connection
        /// </summary>
        public void Open()
        {
            _receiverThread = new Thread(StartServer);
            _receiverThread.Name = "HDrive UDP thread on port: " + _udpPort;
            _receiverThread.Start();
        }

        /// <summary>
        ///     Close the Serial-listener, Aborts all threads
        /// </summary>
        public void Close()
        {
            _searchClients = false;
            _receiverThread.Abort();
            _udpSocket.Close();
        }

        public void StartServer()
        {
            _udpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, _udpPort));
            Console.WriteLine("UDP - Begin receive on port: " + _udpPort + " on: " + _ipAddress);

            while (_searchClients)
            {
                var localHostIpEnd = new IPEndPoint(IPAddress.Any, _udpPort);
                Byte[] receiveBytes = _udpSocket.Receive(ref localHostIpEnd);
                _interpreter("", receiveBytes);
            }
        }
    }
}