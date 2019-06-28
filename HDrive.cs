using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml;
using Utilities;
using WDriveConnection;

namespace HDrive_Driver
{
    public delegate void NewDataArrivedGUI(int number);

    public enum OperationModes
    {
        Error = -1,
        Stop = 0,
        Stepper = 8,
        Calibration = 9,

        NegativeLimitSwitchAdvanced = 15,
        PositivLimitSwitchAdvanced = 16,
        NegativLimitSwitch = 17,
        PositivLimitSwitch = 18,

        TorqueMode = 128,
        PositionControl = 129,
        PositionControl_NPP = 133,
        SpeedControl = 130,
        SpeedControl_NPP = 132
    }

    public enum SystemModes
    {
        SystemReset = 0, // Resets the HDrive
        BootloaderUpgrade = 1, // Starts bootloder update mode
        ResetPosition = 2, // Resets position to 0
        FactoryReset = 3, // Loads the factory defaults
        SaveDataToEEPROM = 4, // Saves all objects into the EEPROM
        ResetLastError = 5 // Resets and confirm the last motor error
    }

    public struct HDriveMotionVariables
    {
        public OperationModes Controlmode;
        public int TargetPosition; // Target position in 1/10° from -32Bit to +32Bit
        public int TargetSpeed; // For HDrive17 from -1500 RPM to +1500 RPM
        public int TargetTorque; // For HDrive17 from -600 mNm to 600 mNm
        public int TargetAcceleration; // Target acceleration in RPM/s^2
        public int TargetDeceleration; // Target deceleration in RPM/s^2
    }

    public class HDrive
    {
        private readonly Stopwatch _timeMeasurment;
        private readonly XmlDocument _xmLdoc;
        private readonly IPAddress _ip;
        private readonly int _ticketID;

        private NewDataArrivedGUI _delegateToGUI;

        private IWDriveConnection _TcpConn;
        private IWDriveConnection _UdpConn;
    
        // The count of received packages used for time measurements
        private int _packagesRecieved;

        public int ID { get; set; }
        public double Uptime { get; set; }
        public double Position { get; set; }
        public int Speed { get; set; }
        public int Current { get; set; }        
        public int PhaseA { get; set; }
        public int PhaseB { get; set; }
        public int Fid { get; set; }
        public int Fiq { get; set; }
        public int DigitalIO { get; set; }

        // This is for test purpose
        public int PrgIndex { get; set; }

        public List<double> SlavePositions { get; set; }
        public List<double> SlaveModes { get; set; }

        // Stors the result of the last read object
        public int LastObjRead { get; set; }

        // A reset event to wait for a get Object request
        private AutoResetEvent waitForObject;

        // Actual motor mode
        public int motorMode { get; set; }

        /// <summary>
        /// Creats an instance of a HDrive
        /// </summary>
        /// <param name="id">The user specific identifier later used to distinguish between multiple HDrive tickets</param>
        /// <param name="ip">The IP Adess to the motor</param>
        /// <param name="delegateToGUI">The callback function pointer </param>
        /// <param name="ticketID">The selected Ticket ID used for binary tickets 1 = binary, 0 = text </param>
        public HDrive(int id, IPAddress ip, int ticketID = 0)
        {
            ID = id;
            _ip = ip;

            _ticketID = ticketID;

            _timeMeasurment = new Stopwatch();
            _xmLdoc = new XmlDocument();

            SlavePositions = new List<double>(8) { 0, 0, 0, 0, 0, 0, 0, 0 };
            SlaveModes = new List<double>(8) { 0, 0, 0, 0, 0, 0, 0, 0 };
        }

        /// <summary>
        /// Esthablish a TCP connection to the HDrive. Waits 5 seconds and aborts then if no HDrive is ansering.
        /// </summary>
        /// <param name="tcpPort">The TCP Port of the HDrive</param>
        /// <param name="udpPort">The UDP Port of the HDrive if UDP is ssetup on the HDrive to be used</param>
        public void Connect(int tcpPort, NewDataArrivedGUI delegateToGUI, int udpPort = 0)
        {
            AutoResetEvent isConnected = new AutoResetEvent(false);
            _delegateToGUI = delegateToGUI;

            var simpleDelegate = new NewDataFromSerialArrived(TicketInterpreter);
            _TcpConn = new TCPConnection(_ip, simpleDelegate, tcpPort, isConnected);
            _TcpConn.Open();

            if (udpPort != 0)
            {
                _UdpConn = new UDPConnection(_ip, simpleDelegate, udpPort, isConnected);
                _UdpConn.Open();
            }

            // Wait untill the HDrive is connected
            isConnected.WaitOne();
        }

        private void TicketInterpreter(string data, byte[] buffer)
        {
            // This is binary UDP ticket
            if (buffer.Length > 0)
            {
                Int32[] pos = new Int32[buffer.Length];
                Buffer.BlockCopy(buffer, 0, pos, 0, buffer.Length);

                if (_ticketID == 1) // Binary CAN ticket
                {
                    Uptime = pos[0];
                    Position = pos[1];
                    SlavePositions[0] = pos[2];
                    SlavePositions[1] = pos[3];
                    SlavePositions[2] = pos[4];
                    SlavePositions[3] = pos[5];
                    SlavePositions[4] = pos[6];
                    SlavePositions[5] = pos[7];
                    SlavePositions[6] = pos[8];

                    motorMode = pos[9];
                    SlaveModes[0] = pos[10];
                    SlaveModes[1] = pos[11];
                    SlaveModes[2] = pos[12];
                    SlaveModes[3] = pos[13];
                    SlaveModes[4] = pos[14];
                    SlaveModes[5] = pos[15];
                    SlaveModes[6] = pos[16];
                    DigitalIO = pos[17];
                    PrgIndex = pos[18];
                }
                else
                {
                    Uptime = pos[0];
                    Position = pos[1];
                    Speed = pos[2] / 10;

                    PhaseA = pos[3];
                    PhaseB = pos[4];

                    Fid = pos[6];
                    Fiq = pos[7];
                }

                _delegateToGUI(ID);
            }

            //this is XML TCP ticket
            else
            {
                XmlElement _root;
                string xml = data.Replace("\0", "");
                try
                {
                    _xmLdoc.InnerXml = xml;
                    _root = _xmLdoc.DocumentElement;

                    if (_root != null && _root.Name == "objRead")
                    {
                        LastObjRead = Convert.ToInt32(_root.GetAttribute("value"));
                        waitForObject.Set();
                    }

                    if (_root != null && _root.Name == "HDrive")
                    {
                        if (_root.HasAttribute("Position"))
                            Position = Convert.ToInt32(_root.GetAttribute("Position")) / 10.0;
                        if (_root.HasAttribute("Speed"))
                            Speed = Convert.ToInt32(_root.GetAttribute("Speed")) / 10;
                        if (_root.HasAttribute("Torque"))
                            Current = Convert.ToInt32(_root.GetAttribute("Current"));
                        if (_root.HasAttribute("Time"))
                            Uptime = Convert.ToDouble(_root.GetAttribute("Time")) / 1000.0;

                        _delegateToGUI(ID);
                    }
                    if (_root != null && _root.Name == "CANTicket")
                    {
                        if (_root.HasAttribute("Position"))
                            Position = Convert.ToInt32(_root.GetAttribute("Position")) / 10.0;

                        // Collect CAN slave data
                        for (int ii = 1; ii < 9; ++ii)
                        {
                            if (_root.HasAttribute("posS" + ii))
                                SlavePositions[ii - 1] = Convert.ToInt32(_root.GetAttribute("posS" + ii));
                            if (_root.HasAttribute("mS" + ii))
                                SlaveModes[ii - 1] = Convert.ToInt32(_root.GetAttribute("mS" + ii));
                        }

                        if (_root.HasAttribute("Mode"))
                            motorMode = Convert.ToInt32(_root.GetAttribute("Mode"));
                        if (_root.HasAttribute("time"))
                            Uptime = Convert.ToDouble(_root.GetAttribute("time")) / 1000.0;

                        _delegateToGUI(ID);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("drive: " + ID + "this is not xml:" + xml + "err msg: " + e);
                }
            }

            // Measure ticket time
            if (++_packagesRecieved > 1000)
            {
                _timeMeasurment.Stop();

                Debug.WriteLine("drive: " + ID + " average recievtime: ms " + Math.Round(1.0 / (_timeMeasurment.ElapsedMilliseconds / (double)_packagesRecieved), 3) + "KHz");
                _packagesRecieved = 0;
                _timeMeasurment.Restart();
            }
        }

        /// <summary>
        /// Stops the motor
        /// </summary>
        public void Stop()
        {
            string s = "<control pos=\"0\" speed=\"0\" torque=\"0\" mode=\"0\" acc=\"0\" dcc=\"0\" />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Executes a drive motion towards a given position in respect of its acceleration, decceleration and profile speed.
        /// Depending on the selected mode the accelerations and profile speed are getting ignored.
        /// </summary>
        /// <param name="motionVariables">The motion variables</param>
        public void GoToPosition(HDriveMotionVariables motionVariables)
        {
            string s = "<control pos=\"" + motionVariables.TargetPosition + "\" speed=\"" + motionVariables.TargetSpeed + "\" torque=\"" + motionVariables.TargetTorque +
                      "\" mode=\"" + motorMode + "\" acc=\"" + motionVariables.TargetAcceleration + "\" dcc=\"" + motionVariables.TargetDeceleration + "\" />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Reads and blocks untill an answer is received.
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slave"></param>
        /// <returns>The value of the asked object</returns>
        public int ReadObject(int master, int slave)
        {
            string str = "<objRead m=\"" + master + "\" s=\"" + slave + "\" >";
            _TcpConn.Write(str);

            // Wait now untill the motor has answered with the object
            waitForObject = new AutoResetEvent(false);
            waitForObject.WaitOne();

            return LastObjRead;
        }

        /// <summary>
        /// Writes an object from the object dictionary on the drive.
        /// Cauntion: writing wrong data to objects can damage the motor.
        /// </summary>
        /// <param name="m">Master object index</param>
        /// <param name="s">Slave object index</param>
        /// <param name="value"></param>
        public void WriteObject(int m, int s, int value)
        {
            string str = "<objWrite m=\"" + m + "\" s=\"" + s + "\" x=\"" + value + "\" />";
            _TcpConn.Write(str);
        }

        /// <summary>
        /// Sets multiple target position to a CAN master and its slaves
        /// </summary>
        /// <param name="positionMaster">Master position in 1/10 degree</param>
        /// <param name="positionSlave1">Slave1 position in 1/10 degree</param>
        /// <param name="positionSlave2">Slave2 position in 1/10 degree</param>
        /// <param name="positionSlave3">Slave3 position in 1/10 degree</param>
        /// <param name="positionSlave4">Slave4 position in 1/10 degree</param>
        /// <param name="positionSlave5">Slave5 position in 1/10 degree</param>
        /// <param name="positionSlave6">Slave6 position in 1/10 degree</param>
        /// <param name="positionSlave7">Slave7 position in 1/10 degree</param>
        /// <param name="positionSlave8">Slave8 position in 1/10 degree</param>
        public void GoToPositionCAN(long positionMaster, long positionSlave1, long positionSlave2, long positionSlave3, long positionSlave4, long positionSlave5, long positionSlave6, long positionSlave7, long positionSlave8)
        {
            string s = "<canPos m=\"" + positionMaster + "\" s=\"" + positionSlave1 + "\" s=\"" + positionSlave2 +
                       "\" s=\"" + positionSlave3 + "\" s=\"" + positionSlave4 + "\" s=\"" + positionSlave5 + "\"  s=\"" + positionSlave6 + "\"  s=\"" + positionSlave7 + "\"  s=\"" + positionSlave8 + "\" />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// This configures the CAN master and its slaves
        /// </summary>
        /// <param name="torqueM">Master torque</param>
        /// <param name="modeM">Master mode</param>
        /// <param name="cS1">Slave 1 torque</param>
        /// <param name="cS2">Slave 2 torque</param>
        /// <param name="cS3">Slave 3 torque</param>
        /// <param name="cS4">Slave 4 torque</param>
        /// <param name="cS5">Slave 5 torque</param>
        /// <param name="cS6">Slave 6 torque</param>
        /// <param name="cS7">Slave 7 torque</param>
        /// <param name="cS8">Slave 8 torque</param>
        /// <param name="mS1">Slave 1 mode</param>
        /// <param name="mS2">Slave 2 mode</param>
        /// <param name="mS3">Slave 3 mode</param>
        /// <param name="mS4">Slave 4 mode</param>
        /// <param name="mS5">Slave 5 mode</param>
        /// <param name="mS6">Slave 6 mode</param>
        /// <param name="mS7">Slave 7 mode</param>
        /// <param name="mS8">Slave 8 mode</param>
        public void ConfigCAN(int torqueM, OperationModes modeM, int cS1, int cS2, int cS3, int cS4, int cS5, int cS6, long cS7, int cS8,
            OperationModes mS1, OperationModes mS2, OperationModes mS3, OperationModes mS4, OperationModes mS5, OperationModes mS6, OperationModes mS7, OperationModes mS8)
        {
            string s = "<canConf m=\"" + torqueM + "\" m=\"" + (int)modeM +
              "\" s =\"" + cS1 + "\" s=\"" + cS2 + "\" s=\"" + cS3 + "\" s=\"" + cS4 + "\" s=\"" + cS5 + "\" s=\"" + cS6 + "\" s=\"" + cS7 + "\" s=\"" + cS8 +
              "\" s =\"" + (int)mS1 + "\" s=\"" + (int)mS2 + "\" s=\"" + (int)mS3 + "\" s=\"" + (int)mS4 + "\" s=\"" + (int)mS5 + "\" s=\"" + (int)mS6 + "\" s=\"" + (int)mS7 + "\" s=\"" + (int)mS8 + "\" />";
            _TcpConn.Write(s);

            Thread.Sleep(2);
        }

        /// <summary>
        /// Loads a specific configuration to the CAN-master and slave modulss
        /// </summary>
        /// <param name="master_speed">Master profile speed in RPM</param>
        /// <param name="master_acc">Master profile acceleration in RPM/s^2</param>
        /// <param name="master_decc">Master profile deceleration in  RPM/s^2</param>
        /// <param name="s1">Slave 1 profile speed in RPM</param>
        /// <param name="a1">Slave 1 profile acceleration in RPM/s^2</param>
        /// <param name="d1">Slave 1 profile deceleration in RPM/s^2</param>
        /// <param name="s2">Slave 2 profile speed in RPM</param>
        /// <param name="a2">Slave 2 profile acceleration in RPM/s^2</param>
        /// <param name="d2">Slave 2 profile deceleration in RPM/s^2</param>
        /// <param name="s3">Slave 3 profile speed in RPM</param>
        /// <param name="a3">Slave 3 profile acceleration in RPM/s^2</param>
        /// <param name="d3">Slave 3 profile deceleration in RPM/s^2</param>
        /// <param name="s4">Slave 4 profile speed in RPM</param>
        /// <param name="a4">Slave 4 profile acceleration in RPM/s^2</param>
        /// <param name="d4">Slave 4 profile deceleration in RPM/s^2</param>
        /// <param name="s5">Slave 5 profile speed in RPM</param>
        /// <param name="a5">Slave 5 profile acceleration in RPM/s^2</param>
        /// <param name="d5">Slave 5 profile deceleration in RPM/s^2</param>
        /// <param name="s6">Slave 6 profile speed in RPM</param>
        /// <param name="a6">Slave 6 profile acceleration in RPM/s^2</param>
        /// <param name="d6">Slave 6 profile deceleration in RPM/s^2</param>
        /// <param name="s7">Slave 7 profile speed in RPM</param>
        /// <param name="a7">Slave 7 profile acceleration in RPM/s^2</param>
        /// <param name="d7">Slave 7 profile deceleration in RPM/s^2</param>
        /// <param name="s8">Slave 8 profile speed in RPM</param>
        /// <param name="a8">Slave 8 profile acceleration in RPM/s^2</param>
        /// <param name="d8">Slave 8 profile deceleration in RPM/s^2</param>
        public void AdvancedConfigCAN(int master_speed, int master_acc, int master_decc, int s1, int a1, int d1, int s2, int a2, int d2, int s3, int a3, int d3
            , int s4, int a4, int d4, int s5, int a5, int d5, int s6, int a6, int d6, int s7, int a7, int d7, int s8, int a8, int d8)
        {
            string s = "<canC2 m=\"" + master_speed + "\" m=\"" + master_acc + "\" m =\"" + master_decc +
              "\" s=\"" + s1 + "\" s=\"" + a1 + "\" s=\"" + d1 + "\" s=\"" + s2 + "\" s=\"" + a2 + "\" s=\"" + d2 +
              "\" s=\"" + s3 + "\" s =\"" + a3 + "\" s=\"" + d3 + "\" s=\"" + s4 + "\" s=\"" + a4 + "\" s=\"" + d4 +
              "\" s=\"" + s5 + "\" s =\"" + a5 + "\" s=\"" + d5 + "\" s=\"" + s6 + "\" s=\"" + a6 + "\" s=\"" + d6 +
              "\" s=\"" + s7 + "\" s =\"" + a7 + "\" s=\"" + d7 + "\" s=\"" + s8 + "\" s=\"" + a8 + "\" s=\"" + d8 +
              "\" />";
            _TcpConn.Write(s);

            Thread.Sleep(2);
        }

        /// <summary>
        /// Implemented for testing purpose. Not implemented yet in firmware yet
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="R"></param>
        /// <param name="startA"></param>
        /// <param name="stopA"></param>
        /// <param name="acc"></param>
        /// <param name="decc"></param>
        /// <param name="vramp"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        public void ConfigPath(int id, int x, int y, int z, int R, int startA, int stopA, int acc, int decc, int vramp, int v0, int v1)
        {
            string s = "<canProf id=\"" + id + "\" x=\"" + x + "\" y=\"" + y + "\" z =\"" + z + "\" R=\"" + R + "\" startA=\"" + startA + "\" stopA=\"" + stopA + "\" acc =\"" + acc + "\" decc =\"" + decc + "\" vramp =\"" + vramp + "\" v0=\"" + v0 + "\" v1=\"" + v1 + "\" />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Implemented for testing purpose.  Not implemented yet in firmware yet
        /// </summary>
        /// <param name="id"></param>
        /// <param name="points"></param>
        public void ConfigPathPoints(int id, List<Double3> points)
        {
            string s = "<cantraj id=\"" + id + "\"";

            for (int ii = 0; ii < points.Count; ++ii)
                s += " x=\"" + (int)(points[ii].X * 1000) + "\" y=\"" + (int)(points[ii].Y * 1000) + "\" z=\"" + (int)(points[ii].Z * 1000) + "\"";

            s += " />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Implemented for testing purpose.  Not implemented yet in firmware yet
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="a1"></param>
        /// <param name="d1"></param>
        /// <param name="s2"></param>
        /// <param name="a2"></param>
        /// <param name="d2"></param>
        /// <param name="s3"></param>
        /// <param name="a3"></param>
        /// <param name="d3"></param>
        /// <param name="s4"></param>
        /// <param name="a4"></param>
        /// <param name="d4"></param>
        /// <param name="s5"></param>
        /// <param name="a5"></param>
        /// <param name="d5"></param>
        /// <param name="s6"></param>
        /// <param name="a6"></param>
        /// <param name="d6"></param>
        /// <param name="s7"></param>
        /// <param name="a7"></param>
        /// <param name="d7"></param>
        /// <param name="s8"></param>
        /// <param name="a8"></param>
        /// <param name="d8"></param>
        /// <param name="s9"></param>
        /// <param name="a9"></param>
        /// <param name="d9"></param>
        public void ConfigProfileCAN(int s1, int a1, int d1, int s2, int a2, int d2, int s3, int a3, int d3, int s4, int a4, int d4, int s5, int a5, int d5, int s6, int a6, int d6, int s7, int a7, int d7, int s8, int a8, int d8, int s9, int a9, int d9)
        {
            string s = "<canProf m=\"" + s1 + "\" m=\"" + a1 + "\" m =\"" + d1 + "\"" +
                " m=\"" + s2 + "\" m=\"" + a2 + "\" m =\"" + d2 + "\"" +
                " m=\"" + s3 + "\" m=\"" + a3 + "\" m =\"" + d3 + "\"" +
                " m=\"" + s4 + "\" m=\"" + a4 + "\" m =\"" + d4 + "\"" +
                " m=\"" + s5 + "\" m=\"" + a5 + "\" m =\"" + d5 + "\"" +
                " w=\"" + s6 + "\" m=\"" + a6 + "\" m =\"" + d6 + "\"" +
                " x=\"" + s7 + "\" m=\"" + a7 + "\" m =\"" + d7 + "\"" +
                " y=\"" + s8 + "\" m=\"" + a8 + "\" m =\"" + d8 + "\"" +
                " z=\"" + s9 + "\" m=\"" + a9 + "\" m =\"" + d9 + "\"" +
                " />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Resets the motor position to zero. The motor needs arround 200ms to finish this request
        /// </summary>
        public void ResetPositionToZero()
        {
            switchMode(SystemModes.ResetPosition);
        }

        /// <summary>
        /// Switches the motor mode
        /// </summary>
        /// <param name="motormode">The demanded motor mode</param>
        public void switchMode(SystemModes motormode)
        {
            string s = "<system t1=\"" + motormode + "\" t2=\"1\" t3=\"2\" t4=\"3\" />";
            _TcpConn.Write(s);
        }

        /// <summary>
        /// Closes the TCP and UDP connections
        /// </summary>
        public void Close()
        {
            if (_TcpConn != null)
                _TcpConn.Close();
            if (_UdpConn != null)
                _UdpConn.Close();
        }
    }
}
