using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Visualization;
using System.Threading.Tasks;
using System.Diagnostics;
using HDrive;
using static Utilities.NetworkTools;
using static HDrive.HDriveInformation;

namespace HDriveDiscovery
{
    public partial class Form1 : Form
    {
        // This is the timeout for a GetWebRequest
        const int WebRequestTimeoutMs = 5000;

        // Contains all data stored inside the config file of the application
        readonly ConfigFile _storedData;

        // The Search machine to search HDrives in the network
        readonly FindHDrives _hdriveSearcher;

        // The result of the found HDrives in the network
        readonly List<HDriveData> _foundHdrives;

        // List of HDrives that are currently updating
        readonly List<HDriveData> _updatedHDrives;

        int _radialProgressbar1;
        int _radialProgressbar2;

        string _statusLabelText = "";
        string _debugConsoleText = "";

        bool _buttonVisibility = false;
        bool _gridViewVisibility = false;

        // used to check if the List should be updated
        int _oldCountOfHDrives;

        readonly object _locker = new object();
        readonly List<Task> _updateTasks = new List<Task>();
        bool _updateStarted;

        public Form1()
        {
            InitializeComponent();

            _updatedHDrives = new List<HDriveData>();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            _foundHdrives = new List<HDriveData>();
            _storedData = new ConfigFile("config.xml");

            dataGridView.RowHeadersWidth = 4; // the left row header size.
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AutoResizeColumns();

            dataGridView.ColumnCount = 7;
            dataGridView.Columns[0].Name = "IP";
            dataGridView.Columns[1].Name = "MAC";
            dataGridView.Columns[2].Name = "S/N";
            dataGridView.Columns[3].Name = "FW. version";
            dataGridView.Columns[4].Name = "GUI version";
            dataGridView.Columns[5].Name = "Boot version";
            dataGridView.Columns[6].Name = "App ID";

            DataGridViewProgressColumn column = new DataGridViewProgressColumn();
            column.HeaderText = "Progress";
            dataGridView.Columns.Add(column);

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var fwUpdate = new DataGridViewLinkColumn();
            fwUpdate.UseColumnTextForLinkValue = true;
            fwUpdate.HeaderText = "FW update";
            fwUpdate.Text = "update";
            dataGridView.Columns.Add(fwUpdate);

            var guiUpdateButton = new DataGridViewLinkColumn();
            guiUpdateButton.UseColumnTextForLinkValue = true;
            guiUpdateButton.HeaderText = "GUI update";
            guiUpdateButton.Text = "update";
            dataGridView.Columns.Add(guiUpdateButton);

            var uploadConfigFile = new DataGridViewLinkColumn();
            uploadConfigFile.UseColumnTextForLinkValue = true;
            uploadConfigFile.HeaderText = "Upload config";
            uploadConfigFile.Text = "upload";
            dataGridView.Columns.Add(uploadConfigFile);

            dataGridView.Columns[0].Width = 90;
            dataGridView.Columns[1].Width = 120;
            dataGridView.Columns[2].Width = 60;
            dataGridView.Columns[3].Width = 90;
            dataGridView.Columns[4].Width = 90;
            dataGridView.Columns[5].Width = 90;
            dataGridView.Columns[6].Width = 70;
            dataGridView.Columns[7].Width = 70;
            dataGridView.Columns[8].Width = 80;
            dataGridView.Columns[9].Width = 80;
            dataGridView.Columns[10].Width = 80;

            var t1 = new System.Windows.Forms.Timer();
            t1.Tick += UpdateProgressBar;
            t1.Interval = 100;
            t1.Start();

            _buttonVisibility = false;
            dataGridView.Rows.Clear();

            _hdriveSearcher = new FindHDrives(
                int.Parse(_storedData.getParameter("PingTimeout"))
                , int.Parse(_storedData.getParameter("StartIP"))
                , int.Parse(_storedData.getParameter("StopIP"))
                , _storedData.getParameter("BaseIP")
                , int.Parse(_storedData.getParameter("PingSequenceTimeout"))
                , NewHDriveFound
                , HDriveSearchFinished
                );

            _hdriveSearcher.StartPingSweep();
        }

        private void NewHDriveFound(HDriveData data)
        {
            _gridViewVisibility = true;
            _foundHdrives.Add(data);

            _debugConsoleText = " HDrive found: " + data.Ip + "\r\n" + _debugConsoleText;
        }

        private void HDriveSearchFinished(int i)
        {
            _statusLabelText = "Total " + _foundHdrives.Count + " HDrives found";
            _buttonVisibility = true;
        }

        private void UpdateProgressBar(object sender, EventArgs ev)
        {
            // Check if all update tasks are completed
            bool allTasksCompleted = true;
            foreach (Task t in _updateTasks)
            {
                if (!t.IsCompleted)
                    allTasksCompleted = false;
            }

            if (allTasksCompleted && _updateStarted)
            {
                _updateTasks.Clear();
                _updateStarted = false;

                // Re discover drives
                lock (_locker)
                {
                    _oldCountOfHDrives = 0;
                    _foundHdrives.Clear();
                    _buttonVisibility = false;
                    _hdriveSearcher.DetectHDrives();
                }
            }

            if (_foundHdrives.Count != _oldCountOfHDrives)
            {
                dataGridView.Rows.Clear();

                foreach (HDriveData e in _foundHdrives)
                {
                    bool error = false;

                    // Check if bootLoader is active
                    if (e.FwVersion == 0)
                    {
                        try
                        {
                            string errorTxt = new TimedWebClient { Timeout = WebRequestTimeoutMs }.DownloadString("http://" + e.Ip.ToString() + "/getData.cgi?txt");
                            if (errorTxt.Contains("crash") || errorTxt.Contains("not to work") || errorTxt.Contains("reset"))
                                error = true;
                        }
                        catch (Exception err)
                        {
                            _debugConsoleText = err.ToString() + debugConsole;
                        }
                    }
                    var row0 = new object[] {
                        e.Ip.ToString(), e.Mac.ToString(), e.SerialNumber.ToString(), e.FwVersion.ToString(), e.GuiVersion.ToString(), e.BootLoaderVersion.ToString(), e.AppId.ToString(), e.Progress};

                    dataGridView.Rows.Add(row0);

                    if (error)
                        dataGridView.Rows[dataGridView.Rows.Count - 1].DefaultCellStyle.BackColor = Color.Red;  // your color settings 

                    dataGridView.Invalidate();
                }

                _oldCountOfHDrives = _foundHdrives.Count;
            }

            foreach (HDriveData dt in _updatedHDrives)
            {
                int rowIndex = GetRowIndexFromIp(dt.Ip);
                if (rowIndex >= 0)
                    dataGridView.Rows[rowIndex].Cells[7].Value = dt.Progress;
            }

            _radialProgressbar1 = _hdriveSearcher.PingSweep.ProgressBarValue1;
            _radialProgressbar2 = _hdriveSearcher.PingSweep.ProgressBarValue2;

            // progressBar_fwUpdate.Value = progressBarValue;
            statusLabel.Text = _statusLabelText;
            debugConsole.Text = _debugConsoleText;

            btn_upload_webpage.Enabled = _buttonVisibility;
            btn_FW_Update.Enabled = _buttonVisibility;
            btn_refresh.Enabled = _buttonVisibility;
            btn_refreshHDriveData.Enabled = _buttonVisibility;

            dataGridView.Visible = _gridViewVisibility;

            Invalidate();
        }

        private int GetRowIndexFromIp(String ip)
        {
            // Get row id of the task results ip address
            int rowIndex = -1;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells[0].Value.ToString().Equals(ip))
                {
                    rowIndex = row.Index;
                    break;
                }
            }
            return rowIndex;
        }

        private bool SendData(byte[] fileByteArray2, string motorIP, string contentType, string instructionCode, string additionalString = "")
        {
            bool success = true;
            try
            {
                // Create a request using a URL that can receive a post.
                WebRequest request = WebRequest.Create("http://" + motorIP + "/getData.cgi?" + instructionCode);
                // Set the Method property of the request to POST.
                request.Method = "POST";
                // Set the ContentType property of the WebRequest.
                request.ContentType = contentType;
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = fileByteArray2.Length;
                // Get the request stream.
                using (var dataStream = request.GetRequestStream())
                {
                    // Write the data to the request stream.
                    dataStream.Write(fileByteArray2, 0, fileByteArray2.Length);
                    // Close the Stream object.
                    dataStream.Close();
                }
                // Get the response.
                WebResponse response = request.GetResponse();

                _debugConsoleText = DateTime.Now.ToString("h:mm:ss") + " " + additionalString + ((HttpWebResponse)response).StatusDescription + Environment.NewLine + _debugConsoleText;

                // Get the stream containing content returned by the server.
                using (var dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    // Check the Error response. If the error is not 0 then return.
                    var t = responseFromServer.Split(':');
                    if (t.Count() == 2 && t[1].Trim() != "0")
                    {
                        _debugConsoleText = "ERROR: Data transfer missmatch \n\r" + _debugConsoleText;
                        success = false;
                    }

                    // Clean up the streams.
                    reader.Close();
                    dataStream.Close();
                }
                response.Close();
            }
            catch (Exception e)
            {
                _debugConsoleText = "ERROR: sendArrayAsync Exception: " + e + "\n\r" + _debugConsoleText;
                success = false;
            }
            return success;
        }

        private List<byte[]> ComposeArrayBufferToSend(int firmwareVersion, int index, byte[] fileByteArray, string contentType = "")
        {
            int size;
            if (firmwareVersion >= 235)
                size = 0x250;
            else if (firmwareVersion <= 234 && firmwareVersion > 231)
                size = 0x500;
            else
                size = 16384;

            // Get IP-Address from list
            UInt16 count = (UInt16)Math.Ceiling((float)fileByteArray.Length / (float)size);

            byte countL = (byte)(count & 0xFF);
            byte countH = (byte)(count >> 8);

            List<byte[]> arrayRecordsToSend = new List<byte[]>();
            while (index <= count)
            {
                byte indexL = (byte)(index & 0xFF);
                byte indexH = (byte)(index >> 8);

                // Define 4 byte header with index and page count
                byte[] header = { indexL, indexH, countL, countH };

                // also support old versions
                if (firmwareVersion < 235)
                {
                    header[0] = (byte)index;
                    header[1] = (byte)count;
                    header[2] = 0;
                    header[3] = 0;
                }

                byte[] myArray = fileByteArray.Skip(index * size).Take((index + 1) * size).ToArray();
                Array.Resize(ref myArray, size);

                byte[] fileByteArray2 = new byte[size + header.Count()]; // reserve 1MB

                // copy header to new array
                header.CopyTo(fileByteArray2, 0);
                // copy data to new array
                myArray.CopyTo(fileByteArray2, 4);

                arrayRecordsToSend.Add(fileByteArray2);
                ++index;
            }

            return arrayRecordsToSend;
        }

        private void UploadData(List<byte[]> arrayRecordsToSend, string motorIP, string contentType, string instructionCode)
        {
            int count = arrayRecordsToSend.Count;
            int index = 0;

            HDriveData currentDrive = new HDriveData(motorIP, "", 0, 0, 0, 0, 0, "");
            _updatedHDrives.Add(currentDrive);

            foreach (byte[] t in arrayRecordsToSend)
            {
                bool success = SendData(t, motorIP, contentType, instructionCode, motorIP + ": portion " + index + " of " + (count - 1) + " -> ");

                if (!success)
                {
                    _debugConsoleText = "Abord update \n\r" + _debugConsoleText;
                    return;
                }

                // Update progres bar
                currentDrive.Progress = (int)((index - 1) * 100.0 / count);

                // Increment progress
                ++index;

                Thread.Sleep(50);
            }

            _updatedHDrives.Remove(currentDrive);

            if (instructionCode.ToLower().Contains("fir"))
            {
                _statusLabelText = "Switching application";

                // Switch to new firmware// Create a new WebRequest Object to the mentioned URL.
                WebRequest myWebRequest = WebRequest.Create("http://" + motorIP + "/getData.cgi?swi");
                // Set the 'Timeout' property in Milliseconds.
                myWebRequest.Timeout = 1000;

                // This request will throw a WebException if it reaches the timeout limit before it is able to fetch the resource.
                try
                {
                    WebResponse myWebResponse = myWebRequest.GetResponse();
                    myWebResponse.Close();
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }

                myWebRequest.Abort();

                _statusLabelText = "Uploading Firmware finished";
            }
            else if (instructionCode.ToLower().Contains("fil"))
            {
                _statusLabelText = "Restarting drive";
                RestartDrive(motorIP);
                _statusLabelText = "Uploading Webpage finished";
            }
        }

        private List<byte[]> ConfigureDriveForWebPageUploadAndGenerateArrayBuffer(int firmwareVersion, string motorIP, byte[] fileByteArray)
        {
            const int headerLength = 4;
            int length = fileByteArray.Count();

            string output = new TimedWebClient { Timeout = WebRequestTimeoutMs }.DownloadString("http://" + motorIP + "/getData.cgi?clear");

            byte[] fileByteArrayWithHeader = new byte[length + headerLength]; // reserve 1MB

            fileByteArray.CopyTo(fileByteArrayWithHeader, headerLength);

            // Add web page length to first 4 bytes     
            fileByteArrayWithHeader[3] = (byte)((length >> 24) & 0xFF);
            fileByteArrayWithHeader[2] = (byte)((length >> 16) & 0xFF);
            fileByteArrayWithHeader[1] = (byte)((length >> 8) & 0xFF);
            fileByteArrayWithHeader[0] = (byte)(length & 0xFF);

            Array.Resize(ref fileByteArrayWithHeader, length + headerLength);

            return ComposeArrayBufferToSend(firmwareVersion, 0, fileByteArrayWithHeader);
        }

        private void taskSequencerThread()
        {
            // Nothing to do if there arn't any tasks
            if (_updateTasks.Count == 0)
                return;

            Task activeTask = _updateTasks[_updateTasks.Count - 1];

            while (true)
            {
                if (activeTask.Status == TaskStatus.RanToCompletion)
                {
                    // Remove old task out of list
                    _updateTasks.Remove(activeTask);

                    if (_updateTasks.Count <= 0)
                        return;

                    // set new task as active one
                    activeTask = _updateTasks[_updateTasks.Count - 1];
                }
                else if (activeTask.Status == TaskStatus.Created)
                {
                    activeTask.Start();
                }

                Thread.Sleep(100);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var radialProgressbar = new List<int>();
            radialProgressbar.Add(_radialProgressbar1);
            radialProgressbar.Add(_radialProgressbar2);
            radialProgressbar.Add(0);

            int pingTimeout = int.Parse(_storedData.getParameter("PingTimeout"));
            int StartIP = int.Parse(_storedData.getParameter("StartIP"));
            int StopIP = int.Parse(_storedData.getParameter("StopIP"));
            String BaseIP = _storedData.getParameter("BaseIP");
            int PingSequenceTimeout = int.Parse(_storedData.getParameter("PingSequenceTimeout"));

            Bitmap p1 = RadialProgressBar.drawRadialProgressBar(70, _radialProgressbar1, "Sending pings to network " + BaseIP + "" + StartIP + " to " + StopIP + " (" + pingTimeout + ", " + PingSequenceTimeout + ")");
            Bitmap p2 = RadialProgressBar.drawRadialProgressBar(70, _radialProgressbar2, "waiting for answers");

            e.Graphics.DrawImage(p1, new Point(50, 150));
            e.Graphics.DrawImage(p2, new Point(50, 250));

        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewLinkCell)
            {
                // Don't allow to update anything on Drives in bootLoader mode
                if (dataGridView.SelectedRows[0].DefaultCellStyle.BackColor == Color.Red)
                    return;

                // here you can have column reference by using e.ColumnIndex
                DataGridViewLinkCell cell = (DataGridViewLinkCell)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];

                if (cell.ColumnIndex == 8)
                {
                    // Exit if no list entry has been selected
                    if (dataGridView.SelectedRows.Count <= 0)
                        return;

                    byte[] fileByteArray = new byte[0];

                    string FileVersion = "0.0";

                    int runningApp = 7;
                    int.TryParse(dataGridView.Rows[e.RowIndex].Cells[6].Value.ToString(), out runningApp);
                    _statusLabelText = "Uploading firmware file...";

                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.InitialDirectory = _storedData.getParameter("defaultFolderFW");
                        openFileDialog.Filter = "HDrive firmware files (*.bin)|*.bin";
                        openFileDialog.FilterIndex = 2;
                        openFileDialog.RestoreDirectory = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            _storedData.setParameter("defaultFolderFW", openFileDialog.FileName);

                            // Read current running APP
                            if (runningApp == 7)
                            {
                                _statusLabelText = "Motor app could not be read. Abord.";
                                return;
                            }
                            FileVersion = openFileDialog.FileName.Split('.')[0];
                            FileVersion = FileVersion.Substring(FileVersion.Length - 3, 3);

                            Stream fileStream = openFileDialog.OpenFile();
                            fileByteArray = new byte[fileStream.Length];

                            using (BinaryReader reader = new BinaryReader(fileStream))
                            {
                                // Read firmware file and store into byte array.
                                fileByteArray = reader.ReadBytes((int)reader.BaseStream.Length);
                            }
                        }
                    }
                    if (fileByteArray.Length > 0)
                    {
                        int.TryParse(FileVersion, out int fileVersion);
                        List<HDriveData> templist = new List<HDriveData>();
                        foreach (HDriveData hd in _foundHdrives)
                        {
                            if ((string)dataGridView.Rows[e.RowIndex].Cells[0].Value == hd.Ip)
                                templist.Add(hd);
                        }

                        if (dataGridView.SelectedRows[0].DefaultCellStyle.BackColor == Color.Red)
                        {
                            templist[0].FwVersion = 234; // Set this to use 500KB update packages for FW update
                            updateFW(fileByteArray, 0, templist);
                        }
                        else
                            updateFW(fileByteArray, 0, templist);

                    }
                }
                else if (cell.ColumnIndex == 9)
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.InitialDirectory = _storedData.getParameter("defaultFolderWebGUI");
                        openFileDialog.Filter = "HDrive web GUI files (*.htmlmin)|*.htmlmin";
                        openFileDialog.FilterIndex = 2;
                        openFileDialog.RestoreDirectory = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            _storedData.setParameter("defaultFolderWebGUI", openFileDialog.FileName);
                            _statusLabelText = "Uploading Web page...";

                            Stream fileStream = openFileDialog.OpenFile();
                            byte[] fileByteArray = new byte[fileStream.Length];
                            using (BinaryReader reader = new BinaryReader(fileStream))
                            {
                                // Read firmware file and store into byte array.
                                fileByteArray = reader.ReadBytes((int)reader.BaseStream.Length);
                            }

                            dataGridView.SelectedRows[0].Cells[4].Value = "updating...";

                            String motorVersion = dataGridView.Rows[e.RowIndex].Cells[3].Value.ToString();
                            int.TryParse(motorVersion, out int motorV);

                            // Get IP-Address from list
                            String motorIP = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();

                            // Generate Web binary array
                            var byteArray = ConfigureDriveForWebPageUploadAndGenerateArrayBuffer(motorV, motorIP, fileByteArray);

                            // Send byte array
                            Task updater = new Task(() => UploadData(byteArray, motorIP, "", "fil1"));
                            _updateTasks.Add(updater);
                            updater.Start();
                            _updateStarted = true;

                        }
                    }
                }
                else if (cell.ColumnIndex == 10)
                {

                    _statusLabelText = "Uploading config file...";

                    // Get IP-Address from list
                    String motorIP = dataGridView.SelectedRows[0].Cells[0].Value.ToString();

                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.InitialDirectory = _storedData.getParameter("defaultFolderBootloader"); ;
                        openFileDialog.Filter = "HDrive config files (*.cgi)|*.cgi|All files (*.*)|*.*";
                        openFileDialog.FilterIndex = 2;
                        openFileDialog.RestoreDirectory = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            _storedData.setParameter("defaultFolderBootloader", openFileDialog.FileName);

                            //Get the path of specified file
                            var filePath = openFileDialog.FileName;
                            long fileSize = new FileInfo(filePath).Length;
                            byte[] fileByteArray = new byte[fileSize];
                            using (BinaryReader reader = new BinaryReader(openFileDialog.OpenFile()))
                            {
                                // Read firmware file and store into byte array.
                                fileByteArray = reader.ReadBytes((int)reader.BaseStream.Length);
                            }

                            // Create a request using a URL that can receive a post.
                            var request = WebRequest.Create("http://" + motorIP + "/getData.cgi?sav");
                            // Set the Method property of the request to POST.
                            request.Method = "POST";
                            // Set the ContentType property of the WebRequest.
                            request.ContentType = "multipart/form-data";
                            // Set the ContentLength property of the WebRequest.
                            request.ContentLength = fileByteArray.Length;

                            // Get the request stream.
                            using (var dataStream = request.GetRequestStream())
                            {
                                // Write the data to the request stream.
                                dataStream.Write(fileByteArray, 0, fileByteArray.Length);
                                // Close the Stream object.
                                dataStream.Close();
                            }

                            request.Abort();

                            _statusLabelText = "Objects uploaded. Sending save to EEPROM command";

                            // Give some time to the motor
                            Thread.Sleep(500);

                            // Tell the motor to save its parameters to its EEPROM
                            var request2 = WebRequest.Create("http://" + motorIP + "/writeTicket.cgi");
                            // Set the Method property of the request to POST.
                            request2.Method = "POST";

                            // System mode 4 triggers the eeprom to save all RAM object data
                            String toSend = "<system mode=\"4\" b=\"0\" c=\"0\" d=\"0\" />";

                            // Set the ContentLength property of the WebRequest.
                            request2.ContentLength = toSend.Length;

                            using (var dataStream = request2.GetRequestStream())
                            {
                                // Write the data to the request stream.
                                dataStream.Write(Encoding.ASCII.GetBytes(toSend), 0, toSend.Length);

                                try
                                {
                                    // Close the Stream object.
                                    dataStream.Close();
                                }
                                catch (Exception)
                                {
                                    // This exception ocurse because the motor is automatically restarting after the system mode 4 command
                                }
                            }

                            request2.Abort();
                            dataGridView.SelectedRows[0].Cells[7].Value = 100;
                        }
                        _statusLabelText = "Stored objects in drive EEPROM. Motor is restarting";
                    }
                }
            }
        }

        private void updateBootloader(object sender, EventArgs e)
        {
            ToolStripItem t = (ToolStripItem)sender;


            // Exit if no list entry has been selected
            if (dataGridView.SelectedRows.Count <= 0)
                return;

            _statusLabelText = "Uploading bootLoader file...";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                String storedPath = "";
                String serverIP = "";
                if (Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(prop => prop.Name == "LastBootloaderFilePath"))
                {
                    storedPath = (string)Properties.Settings.Default.Properties["LastBootloaderFilePath"].DefaultValue;
                }
                else
                {
                    storedPath = "C:\\";
                    SettingsProperty prop = new SettingsProperty("LastBootloaderFilePath");
                    prop.PropertyType = typeof(string);
                    Properties.Settings.Default.Properties.Add(prop);
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Properties["LastBootloaderFilePath"].DefaultValue = "c:\\";
                }

                // Read Server IP out of config file. This is mandatory for a correct FW upgrade
                if (Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(prop => prop.Name == "hostIP"))
                {
                    serverIP = (string)Properties.Settings.Default.Properties["hostIP"].DefaultValue;
                }
                else
                {
                    serverIP = "192.168.1.150";
                    SettingsProperty prop = new SettingsProperty("hostIP");
                    prop.PropertyType = typeof(string);
                    Properties.Settings.Default.Properties.Add(prop);
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Properties["hostIP"].DefaultValue = serverIP;
                }


                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "HDrive bootLoader files (*.bin)|*.bin";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default.Properties["LastBootloaderFilePath"].DefaultValue = openFileDialog.FileName;
                    Properties.Settings.Default.Save();

                    _statusLabelText = "Uploading Boot loader...";

                    // Get IP-Address from list
                    String motorMAC = dataGridView.SelectedRows[0].Cells[1].Value.ToString();

                    // Connecto to HDrive
                    HDrive.HDrive h1 = new HDrive.HDrive(0, IPAddress.Parse(t.Name));
                    if (!h1.Connect(1000, (a) => { }, HDriveTicket.HDriveTicket))
                    {
                        _statusLabelText = "Could not connect to drive";
                        return;
                    }

                    // Enter bootLoader mode
                    h1.SwitchMode(SystemModes.BootloaderUpgrade);

                    // Run Bootloder-Update
                    try
                    {
                        Process uploader = System.Diagnostics.Process.Start("LMFlash.exe", " --quick-set=manual --interface=ethernet --net-config=" + serverIP + "," + t.Name + "," + motorMAC + " " + openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    // Release TCP connection to this drive
                    h1.Close();
                }
            }

            _statusLabelText = "Uploading Boot loader finished.";

        }

        private void RestartDrive(string ip, bool bootLoader = false)
        {
            _statusLabelText = "Restart drive...";

            // Tell the motor to save its parameters to its EEPROM
            var request = WebRequest.Create("http://" + ip + "/writeTicket.cgi");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = 512;
            using (var dataStream = request.GetRequestStream())
            {
                // System mode 4 triggers the eeprom to save all RAM object data
                String toSend = "";

                // In bootLoader mode the system command to restart the motor is different than in the FW
                if (bootLoader) toSend = "<system d1=\"0\" d2=\"1\" d3=\"2\" d4=\"3\" />";
                else toSend = "<system mode=\"6\" b=\"1\" c=\"2\" d=\"3\" />";

                // Write the data to the request stream.
                dataStream.Write(Encoding.ASCII.GetBytes(toSend), 0, toSend.Length);
                try
                {
                    // Close the Stream object.
                    dataStream.Close();
                }
                catch (Exception)
                {
                    // This exception is because the motor is automatically restarting after the system mode 4 command
                }
            }
            request.Abort();
        }

        private void RestartDrive(object sender, EventArgs e)
        {
            ToolStripItem t = (ToolStripItem)sender;

            // Exit if no list entry has been selected
            if (dataGridView.SelectedRows.Count <= 0)
                return;

            if (dataGridView.SelectedRows[0].DefaultCellStyle.BackColor == Color.Red)  // your color settings )
                RestartDrive(t.Name, true);
            else
                RestartDrive(t.Name);
        }

        private void CalibrateDrive(object sender, EventArgs e)
        {
            ToolStripItem t = (ToolStripItem)sender;

            // Exit if no list entry has been selected
            if (dataGridView.SelectedRows.Count <= 0)
                return;

            _statusLabelText = "Starting calibration...";

            // Get IP-Address from list
            String motorIP = t.Name;

            // Connecto to HDrive
            HDrive.HDrive h1 = new HDrive.HDrive(0, IPAddress.Parse(motorIP.ToString()));
            if (!h1.Connect(1000, (a) => { }, HDriveTicket.HDriveTicket))
            {
                _statusLabelText = "Could not connect to drive";
                return;
            }

            HDriveMotionVariables mV = new HDriveMotionVariables();
            mV.ControlMode = OperationModes.Calibration;
            h1.GoToPosition(mV);
        }

        private void BlinkDrive(object sender, EventArgs e)
        {
            ToolStripItem t = (ToolStripItem)sender;

            // Connecto to HDrive
            HDrive.HDrive h1 = new HDrive.HDrive(0, IPAddress.Parse(t.Name));
            if (!h1.Connect(1000, (a) => { }, HDriveTicket.HDriveTicket))
            {
                _statusLabelText = "Could not connect to drive";
                return;
            }

            // Set blink speed
            h1.WriteObject(4, 33, 5);

            // Release TCP connection to this drive
            h1.Close();
        }

        private void dataGridView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int currentMouseOverRow = dataGridView.HitTest(e.X, e.Y).RowIndex;
                if (currentMouseOverRow < 0)
                    return;

                var hti = dataGridView.HitTest(e.X, e.Y);
                dataGridView.ClearSelection();
                dataGridView.Rows[hti.RowIndex].Selected = true;

                String ip = (string)dataGridView.Rows[currentMouseOverRow].Cells[0].Value;

                ContextMenuStrip m = new ContextMenuStrip();
                m.Items.Insert(0, new ToolStripLabel(ip) { Font = new Font(DefaultFont, FontStyle.Bold) });

                // ToolStripMenuItem updateBootloaderItem = new ToolStripMenuItem("Bootloader update", null, new EventHandler(updateBootloader), ip);
                // m.Items.Add(updateBootloaderItem);
                ToolStripMenuItem restartDriveItem = new ToolStripMenuItem("Restart Drive", null, new EventHandler(RestartDrive), ip);
                m.Items.Add(restartDriveItem);
                ToolStripMenuItem calibrateDriveItem = new ToolStripMenuItem("Calibrate Drive", null, new EventHandler(CalibrateDrive), ip);
                m.Items.Add(calibrateDriveItem);
                ToolStripMenuItem blinkItem = new ToolStripMenuItem("Blink  ", null, new EventHandler(BlinkDrive), ip);
                m.Items.Add(blinkItem);

                m.Show(dataGridView, new Point(e.X, e.Y));
            }
        }

        #region buttons

        private void btn_Webpage_Batch_Upload_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to update the Web-GUI on all connected HDrives?", "please confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return;
            }

            // Exit if no list entry has been selected
            if (dataGridView.SelectedRows.Count <= 0)
                return;

            _statusLabelText = "Select webpage file...";
            byte[] fileByteArray = new byte[0];

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = _storedData.getParameter("defaultFolderWebGUI");
                openFileDialog.Filter = "HDrive web GUI files (*.htmlmin)|*.htmlmin";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _storedData.setParameter("defaultFolderWebGUI", openFileDialog.FileName);
                    _statusLabelText = "Uploading Web page...";

                    String FileVersion = openFileDialog.FileName.Split('.')[0];
                    FileVersion = FileVersion.Substring(FileVersion.Length - 3, 3);
                    Stream fileStream = openFileDialog.OpenFile();

                    fileByteArray = new byte[fileStream.Length]; // reserve 1MB
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        // Read firmware file and store into byte array.
                        fileByteArray = reader.ReadBytes((int)reader.BaseStream.Length);
                    }
                }
            }

            if (fileByteArray.Length > 0)
            {
                foreach (HDriveData hd in _foundHdrives)
                {
                    Double.TryParse(hd.GuiVersion.Replace('.', ','), out double GUIVersionLocal);
                    var byteArray = ConfigureDriveForWebPageUploadAndGenerateArrayBuffer(hd.FwVersion, hd.Ip, fileByteArray);

                    // Create the update task, but don't run it now
                    Task updater = new Task(() => UploadData(byteArray, hd.Ip, "", "fil1"));

                    // Add the Task to the updatTask list
                    _updateTasks.Add(updater);

                    // Indicate that there is a mass update on going
                    _updateStarted = true;
                }

                // Start the Task sequencer to run the tasks one by one and not parallel. Parallel updates of the HDrives can cause problems.
                Thread taskSequencer = new Thread(taskSequencerThread);
                taskSequencer.Start();
            }
        }

        private void btn_FW_Batch_Update_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to update the Firmware on all connected HDrives?", "please confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return;
            }

            // Exit if no list entry has been selected
            if (dataGridView.SelectedRows.Count <= 0)
                return;

            byte[] fileByteArray = new byte[0];

            string FileVersion = "0.0";

            _statusLabelText = "Uploading firmware file...";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = _storedData.getParameter("defaultFolderFW");
                openFileDialog.Filter = "HDrive firmware files (*.bin)|*.bin";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                // Get IP-Address from list
                String motorIP = dataGridView.SelectedRows[0].Cells[0].Value.ToString();
                int runningApp = 7;
                int.TryParse(dataGridView.SelectedRows[0].Cells[6].Value.ToString(), out runningApp);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _storedData.setParameter("defaultFolderFW", openFileDialog.FileName);

                    // Read current running APP
                    if (runningApp == 7)
                    {
                        _statusLabelText = "Motor app could not be read. Abord.";
                        return;
                    }
                    FileVersion = openFileDialog.FileName.Split('.')[0];
                    FileVersion = FileVersion.Substring(FileVersion.Length - 3, 3);

                    Stream fileStream = openFileDialog.OpenFile();
                    fileByteArray = new byte[fileStream.Length];

                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        // Read firmware file and store into byte array.
                        fileByteArray = reader.ReadBytes((int)reader.BaseStream.Length);
                    }
                }
            }

            if (fileByteArray.Length > 0)
            {
                int.TryParse(FileVersion, out int fileVersion);
                updateFW(fileByteArray, fileVersion, _foundHdrives);
            }


        }

        private void updateFW(byte[] fileByteArray, int fileVersion, List<HDriveData> hdrives)
        {
            // Seperate FWA and FWB
            // Reader FW header A
            byte[] headerA = fileByteArray.Take(16).ToArray();
            var sizeA = ((headerA[12]) |
                (headerA[13] << 8) |
                (headerA[14] << 16) |
                (headerA[15] << 24));

            // Read header B
            byte[] headerB = fileByteArray.Skip(sizeA + 16).Take(sizeA + 32).ToArray();
            var sizeB = ((headerB[12]) |
                (headerB[13] << 8) |
                (headerB[14] << 16) |
                (headerB[15] << 24));

            byte[] fileByteArrayFWA = fileByteArray.Take(sizeA + 16).ToArray();
            byte[] fileByteArrayFWB = fileByteArray.Skip(sizeA + 16).Take(sizeA + 16 + sizeB + 16).ToArray();

            foreach (HDriveData hd in hdrives)
            {
                // Only update if update is necessary
                if (hd.FwVersion != fileVersion)
                {
                    var byteArray = new List<byte[]>();

                    // Decide which firmware should be flashed
                    if (hd.AppId == 1)
                        byteArray = ComposeArrayBufferToSend(hd.FwVersion, 0, fileByteArrayFWB);
                    else if (hd.AppId == 2 | hd.AppId == 0 | hd.AppId == -1)
                        byteArray = ComposeArrayBufferToSend(hd.FwVersion, 0, fileByteArrayFWA);

                    Task prepairDrive = new Task(() => new TimedWebClient { Timeout = WebRequestTimeoutMs }.DownloadString("http://" + hd.Ip + "/getData.cgi?fdel"));
                    Task updater = new Task(() => UploadData(byteArray, hd.Ip, "", "fir"));

                    // The sequencer is working bottom up, therfore push first update task then prepair task
                    _updateTasks.Add(updater);
                    _updateTasks.Add(prepairDrive);
                    _updateStarted = true;
                }
            }

            // Start the Task sequencer to run the tasks one by one and not parallel. Parallel updates of the HDrives can cause problems.
            Thread taskSequencer = new Thread(taskSequencerThread);
            taskSequencer.Start();
        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            lock (_locker)
            {
                _oldCountOfHDrives = 0;
                _foundHdrives.Clear();
                dataGridView.Rows.Clear();
                _buttonVisibility = false;
                _gridViewVisibility = false;
                dataGridView.Rows.Clear();
                _hdriveSearcher.StartPingSweep();
                _statusLabelText = "Searching network...";
            }
        }

        private void btn_detectHDrives(object sender, EventArgs e)
        {
            _oldCountOfHDrives = 0;
            _foundHdrives.Clear();
            _buttonVisibility = false;
            _gridViewVisibility = false;
            _hdriveSearcher.DetectHDrives();
        }

        private void btn_clearLog_Click(object sender, EventArgs e)
        {
            _debugConsoleText = "";
        }

        #endregion

    };
}