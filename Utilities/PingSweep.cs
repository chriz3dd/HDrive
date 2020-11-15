using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class PingSweep
    {
        public struct IpScanJobResult
        {
            public int id;
            public PingReply result;
            public string[] MAC;
            public string IP;

            public IpScanJobResult(int id, PingReply result, string[] MAC, string IP)
            {
                this.id = id;
                this.result = result;
                this.MAC = MAC;
                this.IP = IP;
            }
        }

        static readonly object lockObj = new object();

        private readonly List<IpScanJobResult> _sweepResult;

        public String StatusLabelText = "";
        public String DebugConsoleText = "";

        public int ProgressBarValue1;
        public int ProgressBarValue2;

        readonly int _pingTimeout;
        readonly int _startIp;
        readonly int _stopIp;
        readonly String _baseIp;
        readonly int _pingSequenceTimeout;
        private readonly bool _isRunning = false;
        private readonly Action<int> _searchFinished;

        public List<IpScanJobResult> GetSweepResults()
        {
            return new List<IpScanJobResult>(_sweepResult);
        }

        public PingSweep(int pingTimeout, int startIP, int stopIP, string baseIP, int pingSequenceTimeout, Action<int> pingSearchFinished)
        {
            this._searchFinished = pingSearchFinished;
            this._pingTimeout = pingTimeout;
            _startIp = startIP;
            _stopIp = stopIP;
            _baseIp = baseIP;
            _pingSequenceTimeout = pingSequenceTimeout;

            _sweepResult = new List<IpScanJobResult>();
        }


        public void StartSweep()
        {
            if (!_isRunning)
            {
                Task searchForDrives = new Task(RunPingSweep_Async);
                searchForDrives.Start();
            }
        }

        private async Task<IpScanJobResult> PingAndUpdateAsync(Ping ping, string ip, int id)
        {
            string[] mac = { "0", "0", "0", "0", "0", "0" };
            var reply = await ping.SendPingAsync(ip, _pingTimeout);

            if (reply.Status == IPStatus.Success)
            {
                lock (lockObj)
                {
                    try
                    {
                        string macString = NetworkTools.getMacByIp(ip);
                        if (!String.IsNullOrEmpty(macString))
                        {
                            mac = macString.Split('-').ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsoleText += System.Environment.NewLine + DateTime.Now.TimeOfDay + " " + ex;
                    }
                }
            }
            return new IpScanJobResult(id, reply, mac, ip);
        }

        public async void RunPingSweep_Async()
        {
            _sweepResult.Clear();

            StatusLabelText = "Task 1 of 2 Sending pings to network segment...";
            ProgressBarValue1 = 0;
            ProgressBarValue2 = 0;

            var tasks = new List<Task<IpScanJobResult>>();

            int pingCount = _stopIp - _startIp;
            int progressIndex = 0;
            for (int ii = _startIp; ii <= _stopIp; ++ii)
            {
                var task = PingAndUpdateAsync(new Ping(), _baseIp + ii.ToString(), ii);
                Thread.Sleep(_pingSequenceTimeout);
                tasks.Add(task);

                ProgressBarValue1 = Math.Min((int)((++progressIndex / (float)pingCount) * 100.0), 100);
            }


            StatusLabelText = "Task 2 of 2 wait for IP responses";
            int count = tasks.Count;
            progressIndex = 0;

            // Add a loop to process the tasks one at a time until none remain.
            while (tasks.Count > 0)
            {
                // Show Progress bar
                ProgressBarValue2 = Math.Min((int)((++progressIndex / (float)count) * 100.0), 100);

                // Identify the first task that completes.
                Task<IpScanJobResult> firstFinishedTask = await Task.WhenAny(tasks);

                _sweepResult.Add(firstFinishedTask.Result);

                // Remove the selected task from the list
                tasks.Remove(firstFinishedTask);
            }

            StatusLabelText = " " + progressIndex + " Active clients found.";

            _searchFinished(1);
        }
    }
}
