using System;
using System.Configuration;
using System.IO;

namespace HDriveDiscovery
{
    public class ConfigFile
    {
        private ExeConfigurationFileMap configMap;
        private Configuration config;
        private AppSettingsSection section;

        public string getParameter(String p)
        {
            string returnValue = "";

            if (section.Settings[p] != null)
                returnValue = section.Settings[p].Value;

            return returnValue;
        }

        public void setParameter(String p, string value)
        {
            if (section.Settings[p] != null)
            {
                section.Settings[p].Value = value;
                config.Save();
            }
        }

        public ConfigFile(String filename)
        {
            configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = Directory.GetCurrentDirectory() + @"\" + filename;
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            if (!config.HasFile)
            {
                

                // Add an entry to appSettings.
                config.AppSettings.Settings.Add("ConfigVersion", "1");
                config.AppSettings.Settings.Add("hostIP", "192.168.1.150");
                config.AppSettings.Settings.Add("defaultFolderBootloader", "C:\\");
                config.AppSettings.Settings.Add("defaultFolderWebGUI", "C:\\");
                config.AppSettings.Settings.Add("defaultFolderFW", "");
                config.AppSettings.Settings.Add("defaultFolderBootloader", "");
                config.AppSettings.Settings.Add("PingTimeout", "2000");
                config.AppSettings.Settings.Add("PingSequenceTimeout", "5");
                config.AppSettings.Settings.Add("getMotorTimeout", "300");
                config.AppSettings.Settings.Add("MotorTCPPort", "1000");
                config.AppSettings.Settings.Add("BaseIP", "192.168.1.");
                config.AppSettings.Settings.Add("StartIP", "50");
                config.AppSettings.Settings.Add("StopIP", "105");
                

                config.Save();
            }

            section = (AppSettingsSection)config.GetSection("appSettings");

        }

    }
}
