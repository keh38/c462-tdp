using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KLib.IO;

namespace TapDevPlatform
{
    public class TdpAppSettings
    {
        public Rectangle lastPosition = new Rectangle();
        public string lastConfigFile = "";
        public string lastMatlabFile = "";
        public string lastProjectSubject = "";

        private static TdpAppSettings _instance = null;
        private static TdpAppSettings instance
        {
            get
            {
                if (_instance == null)
                {
                    if (File.Exists(FileLocations.SettingsFile))
                    {
                        _instance = Files.XmlDeserialize<TdpAppSettings>(FileLocations.SettingsFile);
                    }
                    else
                    {
                        _instance = new TdpAppSettings();
                    }
                }
                return _instance;
            }
        }

        public static Rectangle LastPosition
        {
            get { return instance.lastPosition; }
            set { instance.lastPosition = value; Save(); }
        }

        public static string LastConfigFile
        {
            get { return instance.lastConfigFile; }
            set { instance.lastConfigFile = value; Save(); }
        }

        public static string LastMatlabFile
        {
            get { return instance.lastMatlabFile; }
            set { instance.lastMatlabFile = value; Save(); }
        }

        public static string LastProjectSubject
        {
            get { return !string.IsNullOrEmpty(instance.lastProjectSubject) ? instance.lastProjectSubject : "Scratch/_anonymous"; }
            set { instance.lastProjectSubject = value; Save(); }
        }

        private static void Save()
        {
            Files.XmlSerialize(_instance, FileLocations.SettingsFile);
        }
    }
}