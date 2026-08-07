using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathWorks.MATLAB.Engine;
using MathWorks.MATLAB.Types;

using Serilog;

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

    }
}
