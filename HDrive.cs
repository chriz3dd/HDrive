using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml;
using WDriveConnection;

namespace hDrive
{
    public delegate void NewDataArrivedGUI(int number);

    public struct HDriveMotionVariables
    {
        public int Controlmode;
        public int TargetPosition;
        public int TargetSpeed;
        public int TargetCurrent;
        public int TargetAcceleration;
    }

    public class HDrive
    {
        private readonly IWDriveConnection TcpConn;
        private readonly IWDriveConnection UdpConn;
        private readonly NewDataArrivedGUI _delegateToGUI;

        public HDriveMotionVariables motorVariables;

        private readonly Stopwatch _timeMeasurment;
        private readonly XmlDocument _xmLdoc;
        public AutoResetEvent _IsConnected;
        private double _packagesRecieved;
        private IPAddress _ip;

        static int number;

        public HDrive(IPAddress ip, NewDataArrivedGUI delegateToGUI, int tcpPort, AutoResetEvent isConnected, int udpPort = 2000)
        {
            ID = number;
            ++number;

            _delegateToGUI = delegateToGUI;
            _IsConnected = isConnected;
            _ip = ip;

            _timeMeasurment = new Stopwatch();
            _xmLdoc = new XmlDocument();

            var simpleDelegate = new NewDataFromSerialArrived(InterpreterTh);
            TcpConn = new TCPConnection(ip, simpleDelegate, tcpPort, _IsConnected);
            UdpConn = new UDPConnection(ip, simpleDelegate, udpPort, _IsConnected);
            TcpConn.Open();
            UdpConn.Open();
        }

        public int ID { get; set; }

        public double Uptime { get; set; }
        public double Position { get; set; }
        public int Speed { get; set; }
        public int Temperature { get; set; }
        public double Voltage { get; set; }
        public int Current { get; set; }
        public int Calib { get; set; }
        public int phaseA { get; set; }
        public int phaseB { get; set; }
        public int fid { get; set; }
        public int fiq { get; set; }

        private XmlElement _root;

        private void InterpreterTh(string data, byte[] buffer)
        {
            //this is binary UDP ticket
            if (buffer.Length > 0)
            {
                Int32[] pos = new Int32[buffer.Length];
                Buffer.BlockCopy(buffer, 0, pos, 0, buffer.Length);

                Uptime = pos[0];
                Position = pos[1] / 10.0;
                Speed = pos[2] / 10;

                phaseA = pos[3];
                phaseB = pos[4];

                fid = pos[6];
                fiq = pos[7];

                _delegateToGUI(ID);
            }

            //this is XML TCP ticket
            else
            {
                string xml = data.Replace("\0", "");
                try
                {
                    _xmLdoc.InnerXml = xml;
                    _root = _xmLdoc.DocumentElement;

                    if (_root != null && _root.Name == "HDrive")
                    {
                        if (_root.HasAttribute("Position"))
                            Position = Convert.ToInt32(_root.GetAttribute("Position")) / 10.0;
                        if (_root.HasAttribute("Speed"))
                            Speed = Convert.ToInt32(_root.GetAttribute("Speed")) / 10;                       
                        if (_root.HasAttribute("Time"))
                            Uptime = Convert.ToDouble(_root.GetAttribute("Time")) / 1000.0;
                        if (_root.HasAttribute("Temp"))
                            Temperature = Convert.ToInt32(_root.GetAttribute("Temp"));
                        if (_root.HasAttribute("Voltage"))
                            Voltage = Convert.ToInt32(_root.GetAttribute("Voltage"));
                        if (_root.HasAttribute("Current"))
                            Current = Convert.ToInt32(_root.GetAttribute("Current"));

                        _delegateToGUI(ID);
                    }

                    else if (_root != null && _root.Name == "driveDebug")
                    {
                        if (_root.HasAttribute("Position"))
                            Position = Convert.ToDouble(_root.GetAttribute("Position")) / 10.0;
                        if (_root.HasAttribute("Speed"))
                            Speed = Convert.ToInt32(_root.GetAttribute("Speed")) / 10;
                        if (_root.HasAttribute("Calib"))
                            Calib = Convert.ToInt32(_root.GetAttribute("Calib"));
                        if (_root.HasAttribute("phaseA"))
                            phaseA = Convert.ToInt32(_root.GetAttribute("phaseA"));
                        if (_root.HasAttribute("phaseB"))
                            phaseB = Convert.ToInt32(_root.GetAttribute("phaseB"));
                        if (_root.HasAttribute("fid"))
                            fid = Convert.ToInt32(_root.GetAttribute("fid"));
                        if (_root.HasAttribute("fiq"))
                            fiq = Convert.ToInt32(_root.GetAttribute("fiq"));
                        if (_root.HasAttribute("Time"))
                            Uptime = Convert.ToDouble(_root.GetAttribute("Time")) / 1000.0;

                        _delegateToGUI(ID);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("drive: " + ID + "this is not xml:" + xml + "err msg: " + e);
                }
            }

            if (++_packagesRecieved > 10000 )
            {
                _timeMeasurment.Stop();

                Console.WriteLine("drive: " + ID + " average recievtime: ms " + Math.Round(1.0 / (_timeMeasurment.ElapsedMilliseconds / _packagesRecieved), 3) + "KHz");
                _packagesRecieved = 0;
                _timeMeasurment.Reset();
                _timeMeasurment.Start();
            }
        }

        public void Stop()
        {
            string s = "<system t1=\"3\" t2=\"0\" t3=\"0\" t4=\"0\" />";
            TcpConn.Write(s);
        }
   
        public void GoToPosition(long position, double speed, double current, double acceleration, int mode)
        {
            string s = "<control pos=\"" + position + "\" speed=\"" + (int)speed + "\" current=\"" + (int)current +
                       "\" mode=\"" + mode + "\" acc=\"" + (int)acceleration + "\" dcc=\"" + (int)acceleration + "\" />";
            TcpConn.Write(s);
        }

        public void GoToPosition(HDriveMotionVariables motionVariables)
        {
            GoToPosition(motionVariables.TargetPosition, motionVariables.TargetSpeed, motionVariables.TargetCurrent, motionVariables.TargetAcceleration, motionVariables.Controlmode);
        }

        public void ConfigDrive(int ppos, int ipos, int dpos, int pspeed,
            double ispeed, double pcurrent, double icurrent)
        {
            string s = "<config ppos=\""
                + ppos +
                "\" ipos=\""
                + ipos +
                "\" dpos=\""
                + dpos +
                "\" psp=\""
                + pspeed +
                "\" isp=\""
                + ispeed +
                "\" pcur=\""
                + pcurrent +
                "\" icur=\""
                + icurrent +
                "\" t1=\"0\" t2=\"0\" t3=\"0\" t4=\"0\" />";

            TcpConn.Write(s);
        }

        public void Reset()
        {
            string s = "<system t1=\"2\" t2=\"0\" t3=\"0\" t4=\"0\" />";
            TcpConn.Write(s);
        }


        public void Execute()
        {
            GoToPosition(motorVariables);
        }


        public void Close()
        {
            if (TcpConn != null)
                TcpConn.Close();
            if (UdpConn != null)
                UdpConn.Close();
        }

        public string getMotorConfig()
        {
            WebClient client = new WebClient();
            String htmlCode = client.DownloadString("http://" + _ip.ToString() + "/getParams.cgi");
            return htmlCode;
        }

    }
}
