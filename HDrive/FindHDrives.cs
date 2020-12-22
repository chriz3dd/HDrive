using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;

namespace HDrive
{
    public class FindHDrives
    {
        public readonly PingSweep PingSweep;
        List<PingSweep.IpScanJobResult> _pingList;

        readonly Action<HDriveInformation.HDriveData> _newHDriveFound;
        readonly Action<int> _finished;

        public FindHDrives(int pingTimeout, int startIP, int stopIP, string baseIP, int pingSequenceTimeout, Action<HDriveInformation.HDriveData> newHDriveFound, Action<int> finished)
        {
            _pingList = new List<PingSweep.IpScanJobResult>();

            this._newHDriveFound = newHDriveFound;
            this._finished = finished;
            PingSweep = new PingSweep(pingTimeout, startIP, stopIP, baseIP, pingSequenceTimeout, PingSearchFinished);
        }

        public void StartPingSweep()
        {
            PingSweep.StartSweep();
        }

        public void DetectHDrives()
        {
            GetHDrivesOutOfPingList();
        }

        private async void GetHDrivesOutOfPingList()
        {
            var hDriveInformationList = new List<Task<HDriveInformation.HDriveData>>();

            foreach (PingSweep.IpScanJobResult result in _pingList)
            {
                HDriveInformation h1 = new HDriveInformation();

                // Also scan for 0x02 as first byte for MAC (0x02 is reserved for internal devices) to identify older HDrives
                if (result.MAC[0] == "70" && result.MAC[1] == "B3" && result.MAC[2] == "D5" && result.MAC[3] == "8C" || result.MAC[0] == "02")
                {
                    String mac = result.MAC[0] + ":" + result.MAC[1] + ":" + result.MAC[2] + ":" + result.MAC[3] + ":" + result.MAC[4] + ":" + result.MAC[5];
                    hDriveInformationList.Add(h1.CollectData(result.IP.ToString(), mac));
                }
            }

            // Add a loop to process the tasks one at a time until none remain.
            while (hDriveInformationList.Count > 0)
            {
                // Identify the first task that completes.
                Task<HDriveInformation.HDriveData> firstFinishedTask = await Task.WhenAny(hDriveInformationList);
                hDriveInformationList.Remove(firstFinishedTask);
                _newHDriveFound(firstFinishedTask.Result);
            }
            this._finished(0);
        }

        private void PingSearchFinished(int i)
        {
            _pingList = PingSweep.GetSweepResults();
            DetectHDrives();

            this._finished(0);
        }
    }
}
