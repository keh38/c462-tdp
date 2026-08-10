using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathWorks.MATLAB.Engine;
using MathWorks.MATLAB.Types;

using Serilog;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TapDevPlatform
{
    public static class MATLAB
    {
        private static dynamic _engine;

        public static bool IsInitialized { get; private set; }

        public async static Task<bool> Initialize()
        {
            IsInitialized = false;
            try
            {
                _engine = await MATLABEngine.StartMATLABAsync();
                _engine.addpath(_engine.genpath(FileLocations.MatlabFolder));
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not start MATLAB engine: {ex.Message}");
            }

            return IsInitialized;
        }

        public static void CleanUp()
        {
            if (IsInitialized)
            {
                try
                {
                    MATLABEngine.TerminateEngineClient();
                }
                catch { }
            }
        }

        public static void AddPath(string path)
        {
            _engine.addpath(_engine.genpath(path));
        }

        public static string RunFunction(string functionName, string dataFilePath)
        {
            string result = "";

            if (IsInitialized)
            {
                try
                {
                    _engine.eval($"{functionName}('{dataFilePath}')");
                    result = "OK";
                }
                catch (Exception ex)
                {
                    result = ex.Message.Replace("\n", Environment.NewLine); // "Error evaluating MATLAB function";
                    Log.Error($"Error evaluating MATLAB function '{functionName}'\n{ex.Message}");
                }
            }
            return result;
        }

        public static void RunGenerator(string mPath)
        {
            var folder = Path.GetDirectoryName(mPath);
            var file = Path.GetFileNameWithoutExtension(mPath);

            _engine.cd(folder);

            var runOpts = new RunOptions() { Nargout = 0 };
            _engine.eval(runOpts, file);
        }

        public static (bool ok, string report) ValidateTrialList(string jsonPath)
        {
            dynamic data = _engine.eval($"tapping.validateTrialList('{jsonPath}')");

            if (!(data is MATLABStruct))
            {
                return (false, "Invalid data returned from MATLAB function");
            }

            bool ok = data.GetField("ok");
            string report = data.GetField("errors");

            return (ok, report);
        }

        public static void PreviewTrialList(string jsonPath)
        {
            var runOpts = new RunOptions() { Nargout = 0 };
            _engine.eval(runOpts, $"tapping.previewTrialList('{jsonPath}')");
        }
    }
}
