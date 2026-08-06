using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C462.Shared;

namespace TapDevPlatform
{
    internal static class FileLocations
    {
        public static string TdpFolder => Path.Combine(SharedFileLocations.EplFolder, "TDP");
        public static string SettingsFile { get { return Path.Combine(TdpFolder, "TdpAppSettings.xml"); } }
    }
}
