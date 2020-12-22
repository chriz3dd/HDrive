using System;
using System.Net;
using System.Threading.Tasks;

namespace HDrive
{
    public class HDriveInformation
    {
        public string DebugConsoleText = "";

        public class HDriveData
        {
            public string Ip;
            public string Mac;
            public int ProtocolVersion;
            public int FwVersion;
            public int SerialNumber;
            public int BootLoaderVersion;
            public int AppId;
            public string GuiVersion;
            public int Progress;

            public HDriveData(string ip, string mac, int protocolVersion, int fWVersion, int serialNumber, int bootLoaderVersion, int appId, string guiVersion, int progress = 0)
            {
                this.Ip = ip;
                this.Mac = mac;
                this.ProtocolVersion = protocolVersion;
                this.FwVersion = fWVersion;
                this.SerialNumber = serialNumber;
                this.BootLoaderVersion = bootLoaderVersion;
                this.AppId = appId;
                this.GuiVersion = guiVersion;
                this.Progress = progress;
            }

        }

        public class WebClientTimeout : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 10 * 60 * 1000;
                return w;
            }
        }

        public string GetBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                var Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                var End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        public async Task<HDriveData> CollectData(string ipAddress, string mac)
        {
            int protocolVersion = 0;
            int fwVersion = 0;
            int serialNumber = 0;
            int bootloderVersion = 0;
            int appId = 0;
            string guiVersion = "N/A";

            DebugConsoleText += System.Environment.NewLine + DateTime.Now.TimeOfDay + " reading Web-Gui version of:" + ipAddress;
            DebugConsoleText += System.Environment.NewLine + DateTime.Now.TimeOfDay + " Reading objects from motor:" + ipAddress;

            try
            {
                using (var client = new WebClientTimeout())
                {
                    string downloadString = client.DownloadString("http://" + ipAddress);
                    guiVersion = GetBetween(downloadString, "Web-Gui version", "</p>");
                }

                using (var client = new WebClientTimeout())
                {
                    int subindex = 22;
                    int index = 4;
                    string downloadString = client.DownloadString("http://" + ipAddress + "/getData.cgi?obj=" + (subindex << 8 | index));
                    int.TryParse(downloadString, out protocolVersion);
                }

                using (var client = new WebClientTimeout())
                {
                    int subindex = 0;
                    int index = 3;
                    string downloadString = client.DownloadString("http://" + ipAddress + "/getData.cgi?obj=" + (subindex << 8 | index));
                    int.TryParse(downloadString, out fwVersion);
                }

                using (var client = new WebClientTimeout())
                {
                    int subindex = 13;
                    int index = 3;
                    string downloadString = client.DownloadString("http://" + ipAddress + "/getData.cgi?obj=" + (subindex << 8 | index));
                    int.TryParse(downloadString, out serialNumber);
                }

                using (var client = new WebClientTimeout())
                {
                    int subindex = 15;
                    int index = 3;
                    string downloadString = client.DownloadString("http://" + ipAddress + "/getData.cgi?obj=" + (subindex << 8 | index));
                    int.TryParse(downloadString, out appId);
                }

                using (var client = new WebClientTimeout())
                {
                    int subindex = 16;
                    int index = 3;
                    string downloadString = client.DownloadString("http://" + ipAddress + "/getData.cgi?obj=" + (subindex << 8 | index));
                    int.TryParse(downloadString, out bootloderVersion);
                }

                if (guiVersion.Length > 10)
                    guiVersion = "N/A";
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new HDriveData(ipAddress, mac, protocolVersion, fwVersion, serialNumber, bootloderVersion, appId, guiVersion);
        }
    }
}
