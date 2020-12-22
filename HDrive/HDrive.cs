using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml;

namespace HDrive
{

    
    public class HDrive
    {
        // This Stopwatch is used to measure the time between received tickets.
        private readonly Stopwatch _timeMeasurement = new Stopwatch();

        // A reset event to wait for the answer when reading  an object from the drive
        private AutoResetEvent _waitForObject = new AutoResetEvent(false);

        // The ticket ID is used that the parser is decrypting the right ticket.
        private HDriveTicket _ticketId;

        // This is the UDP connection handler
        private UdpConnection _udpConn;

        // This is the TCP connection handler
        private TcpConnection _tcpConn;

        // The count of received packages used for time measurements
        private int _packagesReceived;

        // This action is called when new data from the drive has been received.
        private Action<int> _delegateToGui;

        // This field is used to calculate the time difference between two received tickets from the drive.
        private int _u32PrevStartTime;

        // Raw data for debug tickets
        private Int32[] RawData { get; set; }

        // The maximum run time the PWM routine was consuming in us.
        public float MaxRunTime;

        // This is the IP-Address of the drive
        public IPAddress IpAddress { get; }

       // This is the user given Motor ID to identify the responding tickets if multiple drives are used.
        public int Id { get; set; }

        // The up-time in seconds of the drive
        public UInt32 UpTime { get; set; }

        // The current position in degrees of the drive
        public double Position { get; set; }

        // The actual speed in RPM
        public int Speed { get; set; }

        // The actual Torque the motor has
        public int Torque { get; set; }

        // The actual phase current in mA on phase 1
        public int PhaseA { get; set; }

        // The actual Phase current in mA on phase 2
        public int PhaseB { get; set; }

        // The actual id current in mA
        public int Fid { get; set; }

        // the actual iq current in mA
        public int Fiq { get; set; }

        // The actual voltage applied to the drive
        public double Voltage { get; set; }      
        
        // Actual motor mode
        public OperationModes MotorMode { get; set; }

        // Actual motor state
        public int MotorState { get; set; }

        // Stores the slave positions in degree when a HDrive CAN ticket has been received
        public List<double> SlavePositions { get; set; }

        // Stores the slave states when a HDrive CAN ticket has been received
        public List<double> SlaveStates { get; set; }

        // Stores the slave modes
        public List<OperationModes> SlaveModes { get; set; }

        // Stores the result of the last read object
        public int LastObjRead { get; set; }


        /// <summary>
        /// Creates an instance of a HDrive
        /// </summary>
        /// <param name="id">The user specific identifier later used to distinguish between multiple HDrive tickets</param>
        /// <param name="ip">The IP-Address to the motor</param>
        public HDrive(int id, IPAddress ip)
        {
            this.Id = id;
            this.IpAddress = ip;

            RawData = new Int32[65]; // Init 64 Raw data int16 fields
            SlavePositions = new List<double>(8) { 0, 0, 0, 0, 0, 0, 0, 0 };
            SlaveModes = new List<OperationModes>(8) { 0, 0, 0, 0, 0, 0, 0, 0 };
            SlaveStates = new List<double>(8) { 0, 0, 0, 0, 0, 0, 0, 0 };
        }

        /// <summary>
        /// Establish a TCP connection to the HDrive. Waits 5 seconds and aborts then if no HDrive is answering.
        /// </summary>
        /// <param name="tcpPort">The TCP Port of the HDrive</param>
        /// <param name="ticketId"></param>
        /// <param name="udpPort">The UDP Port of the HDrive if UDP is setup on the HDrive to be used</param>
        /// <param name="delegateToGui"></param>
        /// <param name="timeout"></param>
        public bool Connect(int tcpPort, Action<int> delegateToGui, HDriveTicket ticketId = HDriveTicket.HDriveTicket, int udpPort = 0, int timeout = 2000)
        {
            this._ticketId = ticketId;
            this._delegateToGui = delegateToGui;

            // Indicate the communication interface if the ticket is a binary ticket
            bool binaryTicket = ticketId != 0;

            var isConnected = new AutoResetEvent(false);

            // Attache the ticket-interpreter to the communication interface
            var ticketInterpreterDelegate = new Action<string, byte[]>(TicketInterpreter);

            _tcpConn = new TcpConnection(IpAddress, ticketInterpreterDelegate, tcpPort, isConnected, binaryTicket);
            _tcpConn.Open();

            // Use the UDP Port only if a valid UDP-Port has been specified
            if (udpPort != 0 && udpPort < 65535)
            {
                _udpConn = new UdpConnection(IpAddress, ticketInterpreterDelegate, udpPort, isConnected);
                _udpConn.Open();
            }

            // Wait untill the HDrive is connected
            bool connected = isConnected.WaitOne(timeout);
            return connected;
        }

        /// <summary>
        /// This function takes out the information from the received HDrive tickets
        /// </summary>
        /// <param name="data">The XML String</param>
        /// <param name="buffer">The UDP binary array</param>
        private void TicketInterpreter(string data, byte[] buffer)
        {
            var xml = "";

            // This is binary UDP ticket
            if (buffer.Length > 0 && buffer.Length < RawData.Length)
            {
                Buffer.BlockCopy(buffer, 0, RawData, 0, buffer.Length);
            }
            else // This is a XML TCP ticket
            {
                if (String.IsNullOrEmpty(data))
                    return;

                xml = data.Replace("\0", "");
            }

            switch (_ticketId)
            {
                case HDriveTicket.BinaryDebugTicket: // Binary Debug Ticket
                    _delegateToGui(Id);
                    break;

                case HDriveTicket.BinaryDataLoggerTicket:
                    {
                        UInt32 u32StartTimePwm = (UInt32)RawData[0];
                        int u32StartTime = (int)((UInt32)RawData[1]);
                        UInt32 u32RunTime = (UInt32)RawData[2];
                        UInt32 u32WriteTimePwm = (UInt32)RawData[3];

                        int intPeriodTicks = u32StartTime - _u32PrevStartTime;
                        if (intPeriodTicks < 0)
                            intPeriodTicks = 0xFFFF - _u32PrevStartTime + u32StartTime;
                        _u32PrevStartTime = u32StartTime;

                        float startTimeDelay = (float)u32StartTimePwm;
                        float intPeriod = 4 * intPeriodTicks / 120.0f;
                        float intRunTime = u32RunTime / 120.0f;
                        float pwmWriteDelay = u32WriteTimePwm / 120.0f;

                        MaxRunTime = Math.Max(MaxRunTime, intRunTime);
                        _delegateToGui(Id);
                        break;
                    }

                case HDriveTicket.BinaryCANTicket:
                    {
                        UpTime = (UInt32)RawData[0];
                        Position = RawData[1] / 10;
                        SlavePositions[0] = RawData[2] / 10;
                        SlavePositions[1] = RawData[3] / 10;
                        SlavePositions[2] = RawData[4] / 10;
                        SlavePositions[3] = RawData[5] / 10;
                        SlavePositions[4] = RawData[6] / 10;
                        SlavePositions[5] = RawData[7] / 10;
                        SlavePositions[6] = RawData[8] / 10;
                        SlavePositions[7] = RawData[8] / 10;

                        MotorMode = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[9]);
                        SlaveModes[0] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[10]);
                        SlaveModes[1] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[11]);
                        SlaveModes[2] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[12]);
                        SlaveModes[3] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[13]);
                        SlaveModes[4] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[14]);
                        SlaveModes[5] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[15]);
                        SlaveModes[6] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[16]);
                        SlaveModes[6] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[17]);
                        SlaveModes[7] = (OperationModes)Enum.ToObject(typeof(OperationModes), RawData[18]);

                        MotorState = RawData[19];
                        SlaveStates[0] = RawData[20];
                        SlaveStates[1] = RawData[21];
                        SlaveStates[2] = RawData[22];
                        SlaveStates[3] = RawData[23];
                        SlaveStates[4] = RawData[24];
                        SlaveStates[5] = RawData[25];
                        SlaveStates[6] = RawData[26];
                        SlaveStates[7] = RawData[27];

                        PrgIndex = RawData[28];

                        _delegateToGui(Id);
                        break;
                    }

                case HDriveTicket.BinaryTicket:
                    {
                        UpTime = (UInt32)RawData[0];
                        Position = RawData[1];
                        Speed = RawData[2] / 10;

                        PhaseA = RawData[3];
                        PhaseB = RawData[4];

                        Fid = RawData[6];
                        Fiq = RawData[7];

                        Voltage = RawData[11];

                        SlavePositions[0] = RawData[23];
                        SlavePositions[1] = RawData[24];
                        SlavePositions[2] = RawData[25];
                        SlavePositions[3] = RawData[26];
                        SlavePositions[4] = RawData[27];
                        SlavePositions[5] = RawData[28];
                        SlavePositions[6] = RawData[29];
                        SlavePositions[7] = RawData[30];

                        _delegateToGui(Id);
                        break;
                    }

                // This is not a binary ticket.
                case HDriveTicket.HDriveTicket:
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.InnerXml = xml;
                        XmlElement root = xmlDoc.DocumentElement;

                        try
                        {
                            switch (root?.Name)
                            {
                                case "objRead":
                                    LastObjRead = Convert.ToInt32(root.GetAttribute("value"));
                                    _waitForObject.Set();
                                    break;

                                case "HDrive":
                                    if (root.HasAttribute("Position"))
                                        Position = Convert.ToInt32(root.GetAttribute("Position")) / 10;
                                    if (root.HasAttribute("Speed"))
                                        Speed = Convert.ToInt32(root.GetAttribute("Speed"));
                                    if (root.HasAttribute("Torque"))
                                        Torque = Convert.ToInt32(root.GetAttribute("Torque"));
                                    if (root.HasAttribute("Time"))
                                        UpTime = Convert.ToUInt32(root.GetAttribute("Time"));

                                    _delegateToGui(Id);
                                    break;

                                case "CANTicket":
                                    if (root.HasAttribute("Position"))
                                        Position = Convert.ToInt32(root.GetAttribute("Position")) / 10.0;

                                    // Collect CAN slave data
                                    for (int ii = 1; ii < 9; ++ii)
                                    {
                                        if (root.HasAttribute("posS" + ii))
                                            SlavePositions[ii - 1] = 0.1 * Convert.ToInt32(root.GetAttribute("posS" + ii));
                                        if (root.HasAttribute("mS" + ii))
                                            SlaveModes[ii - 1] = (OperationModes)Enum.ToObject(typeof(OperationModes), Convert.ToInt32(root.GetAttribute("mS" + ii)));
                                        if (root.HasAttribute("sS" + ii))
                                            SlaveStates[ii - 1] = Convert.ToInt32(root.GetAttribute("sS" + ii));
                                    }

                                    if (root.HasAttribute("Mode"))
                                        MotorMode = (OperationModes)Enum.ToObject(typeof(OperationModes), Convert.ToInt32(root.GetAttribute("Mode")));
                                    if (root.HasAttribute("State"))
                                        MotorState = Convert.ToInt32(root.GetAttribute("State"));
                                    if (root.HasAttribute("u32Time"))
                                        UpTime = Convert.ToUInt32(root.GetAttribute("u32Time"));
                                    if (root.HasAttribute("prgIdx"))
                                        PrgIndex = Convert.ToInt32(root.GetAttribute("prgIdx"));

                                    _delegateToGui(Id);
                                    break;

                                default:
                                    Debug.WriteLine("HDrive::TicketInterpreter ticket " + root.InnerText + " not found");
                                    break;
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Motor Nr.: " + Id + " throw an error. XML:" + xml + " err msg: " + e);
                        }
                    }
                    break;

                case HDriveTicket.CANTicket:
                    break;
                case HDriveTicket.EPROMConfigTicket:
                    break;
                case HDriveTicket.ObjTableTicket:
                    break;
                case HDriveTicket.UnknownTicket:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Measure ticket time
            if (++_packagesReceived > 1000)
            {
                _timeMeasurement.Stop();

                Debug.WriteLine("drive: " + Id + " average receive time: " + Math.Round(1.0 / (_timeMeasurement.ElapsedMilliseconds / (double)_packagesReceived), 3) + "KHz");
                _packagesReceived = 0;
                _timeMeasurement.Restart();
            }
        }

        /// <summary>
        /// Stops the motor
        /// </summary>
        public void Stop()
        {
            string s = "<control pos=\"0\" speed=\"0\" torque=\"0\" mode=\"0\" acc=\"0\" dcc=\"0\" />";
            _tcpConn.Write(s);
        }

        /// <summary>
        /// Executes a drive motion towards a given position in respect of its acceleration, deceleration and profile speed.
        /// Depending on the selected mode the accelerations and profile speed are getting ignored.
        /// </summary>
        /// <param name="motionVariables">The motion variables</param>
        public void GoToPosition(HDriveMotionVariables motionVariables)
        {
            string s = "<control pos=\"" + motionVariables.TargetPosition + "\" speed=\"" + motionVariables.TargetSpeed + "\" torque=\"" + motionVariables.TargetTorque +
                      "\" mode=\"" + (int)motionVariables.ControlMode + "\" acc=\"" + motionVariables.TargetAcceleration + "\" dcc=\"" + motionVariables.TargetDeceleration + "\" />";
            _tcpConn.Write(s);
        }

        /// <summary>
        /// Reads a drive object and blocks until an answer is received.
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slave"></param>
        /// <param name="timeout_ms"></param>
        /// <returns>The value of the asked object</returns>
        public int ReadObject(int master, int slave, int timeout_ms = 1500)
        {
            // Thread.Sleep(1);
            _waitForObject = new AutoResetEvent(false);

            _tcpConn?.Write("<objRead m=\"" + master + "\" s=\"" + slave + "\" />");

            // Wait now until the motor has answered with the object
            if (!_waitForObject.WaitOne(timeout_ms))
            {
                Console.WriteLine("Timeout!");
                return -1;
            }

            return LastObjRead;
        }

        /// <summary>
        /// Writes a drive object.
        /// Caution: writing wrong data to objects can damage the motor.
        /// </summary>
        /// <param name="m">Master object index</param>
        /// <param name="s">Slave object index</param>
        /// <param name="value"></param>
        public void WriteObject(int m, int s, int value)
        {
            string str = "<objWrite m=\"" + m + "\" s=\"" + s + "\" x=\"" + value + "\" />";
            _tcpConn.Write(str);
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
        public void GoToPositionCan(long positionMaster, long positionSlave1, long positionSlave2, long positionSlave3, long positionSlave4, long positionSlave5, long positionSlave6, long positionSlave7, long positionSlave8)
        {
            string s = "<canPos m=\"" + positionMaster + "\" s=\"" + positionSlave1 + "\" s=\"" + positionSlave2 +
                       "\" s=\"" + positionSlave3 + "\" s=\"" + positionSlave4 + "\" s=\"" + positionSlave5 + "\"  s=\"" + positionSlave6 + "\"  s=\"" + positionSlave7 + "\"  s=\"" + positionSlave8 + "\" />";
            _tcpConn.Write(s);
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
        /// <param name="sF1"></param>
        public void ConfigCan(int torqueM, OperationModes modeM, int cS1, int cS2, int cS3, int cS4, int cS5, int cS6, int cS7, int cS8,
            OperationModes mS1, OperationModes mS2, OperationModes mS3, OperationModes mS4, OperationModes mS5, OperationModes mS6, OperationModes mS7, OperationModes mS8,
            CanSpecialFunction sF1 = 0, CanSpecialFunction sF2 = 0, CanSpecialFunction sF3 = 0, CanSpecialFunction sF4 = 0, CanSpecialFunction sF5 = 0, CanSpecialFunction sF6 = 0, CanSpecialFunction sF7 = 0, CanSpecialFunction sF8 = 0)
        {
            string s = "<canConf m=\"" + torqueM + "\" m=\"" + (int)modeM +
              "\" s =\"" + cS1 + "\" s=\"" + cS2 + "\" s=\"" + cS3 + "\" s=\"" + cS4 + "\" s=\"" + cS5 + "\" s=\"" + cS6 + "\" s=\"" + cS7 + "\" s=\"" + cS8 +
              "\" s =\"" + (int)mS1 + "\" s=\"" + (int)mS2 + "\" s=\"" + (int)mS3 + "\" s=\"" + (int)mS4 + "\" s=\"" + (int)mS5 + "\" s=\"" + (int)mS6 + "\" s=\"" + (int)mS7 + "\" s=\"" + (int)mS8 +
              "\" s =\"" + (int)sF1 + "\" s=\"" + (int)sF2 + "\" s=\"" + (int)sF3 + "\" s=\"" + (int)sF4 + "\" s=\"" + (int)sF5 + "\" s=\"" + (int)sF6 + "\" s=\"" + (int)sF7 + "\" s=\"" + (int)sF8 +
              "\" />";
            _tcpConn.Write(s);

            Thread.Sleep(2);
        }

        /// <summary>
        /// Loads a specific configuration to the CAN-master and slave modules
        /// </summary>
        /// <param name="masterSpeed">Master profile speed in RPM</param>
        /// <param name="masterAcc">Master profile acceleration in RPM/s^2</param>
        /// <param name="masterDecc">Master profile deceleration in  RPM/s^2</param>
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
        public void AdvancedConfigCan(int masterSpeed, int masterAcc, int masterDecc, int s1, int a1, int d1, int s2, int a2, int d2, int s3, int a3, int d3
            , int s4, int a4, int d4, int s5, int a5, int d5, int s6, int a6, int d6, int s7, int a7, int d7, int s8, int a8, int d8)
        {
            string s = "<canC2 m=\"" + masterSpeed + "\" m=\"" + masterAcc + "\" m =\"" + masterDecc +
              "\" s=\"" + s1 + "\" s=\"" + a1 + "\" s=\"" + d1 + "\" s=\"" + s2 + "\" s=\"" + a2 + "\" s=\"" + d2 +
              "\" s=\"" + s3 + "\" s =\"" + a3 + "\" s=\"" + d3 + "\" s=\"" + s4 + "\" s=\"" + a4 + "\" s=\"" + d4 +
              "\" s=\"" + s5 + "\" s =\"" + a5 + "\" s=\"" + d5 + "\" s=\"" + s6 + "\" s=\"" + a6 + "\" s=\"" + d6 +
              "\" s=\"" + s7 + "\" s =\"" + a7 + "\" s=\"" + d7 + "\" s=\"" + s8 + "\" s=\"" + a8 + "\" s=\"" + d8 +
              "\" />";
            _tcpConn.Write(s);

            Thread.Sleep(2);
        }

        /// <summary>
        /// Switches the motor mode, can also be used to set zero position with SystemModes.ResetPosition
        /// </summary>
        /// <param name="motorMode">The demanded motor mode</param>
        public void SwitchMode(SystemModes motorMode)
        {
            string s = "<system t1=\"" + (int)motorMode + "\" t2=\"1\" t3=\"2\" t4=\"3\" />";
            _tcpConn.Write(s);
        }

        /// <summary>
        /// Closes the TCP and UDP connections
        /// </summary>
        public void Close()
        {
            _tcpConn?.Close();
            _udpConn?.Close();
        }

        #region Test functions. These functions are not released in the master firmware yet.

        // This is for testing purpose
        public int PrgIndex { get; set; }

        /// <summary>
        /// Implemented for testing purpose. Not implemented in master firmware yet
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
            _tcpConn.Write(s);
        }

        /// <summary>
        /// Implemented for testing purpose. Not implemented in master firmware yet
        /// </summary>
        /// <param name="id"></param>
        /// <param name="points"></param>
        public void ConfigPathPoints(int id, List<Double3> points)
        {
            string s = "<cantraj id=\"" + id + "\"";

            foreach (Double3 t in points)
                s += " \"" + (int)(t.X * 1000) + "\" \"" + (int)(t.Y * 1000) + "\" \"" + (int)(t.Z * 1000) + "\"";

            s += " />";
            _tcpConn.Write(s);
        }

        /// <summary>
        /// Implemented for testing purpose. Not implemented in master firmware yet
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
        public void ConfigProfileCan(int s1, int a1, int d1, int s2, int a2, int d2, int s3, int a3, int d3, int s4, int a4, int d4, int s5, int a5, int d5, int s6, int a6, int d6, int s7, int a7, int d7, int s8, int a8, int d8, int s9, int a9, int d9)
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
            _tcpConn.Write(s);
        }
        #endregion

    }
}
