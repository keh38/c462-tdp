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
        public static string SettingsFile => Path.Combine(TdpFolder, "TdpAppSettings.xml");
        public static string MatlabFolder => Path.Combine(TdpFolder, "MATLAB");
        public static string ContractPath => Path.Combine(TdpFolder, "Context", "Tapping_TrialList_Contract.md");
        public static string InstructionsPath => Path.Combine(TdpFolder, "Context", "Tapping_ProjectHead_AI_Instructions.md");
        public static string GeneratorFolder => Path.Combine(TdpFolder, "Output");
        public static string CurrentTry => Path.Combine(GeneratorFolder, "Tapping.CurrentTry.json");
    }
}
