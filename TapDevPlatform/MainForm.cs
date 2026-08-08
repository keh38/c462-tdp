using System.Diagnostics;
using System.Reflection;

using Serilog;

using BasicMeasurements;
using C462.Shared;
using C462.Shared.Protocol.DTOs;
using HTS.Tcp;
using HTSController;
using KLib.IO;
using KLib.Net;
using Tapping;
using ScottPlot;
using KLib.Signals;
using System.Runtime;
using Newtonsoft.Json;
using TDP.Api;
using TDP.Security;

namespace TapDevPlatform
{
    public partial class MainForm : Form
    {
        private HTSNetwork _network;

        private string _logPath;
        private string _currentHtsScene;

        private TappingConfiguration _currentConfig;
        private float _plotSampleRate = 48000;

        private string _dataPath;
        private bool _runStarted;
        private bool _endRunStarted;

        private ConversationManager _conversation = null;
        private string _subjectName = "_unnamed";

        // -------------------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------------------
        public MainForm()
        {
            InitializeComponent();
            RestoreLastPosition();

            subjectStatusLabel.Visible = false;
            sceneNameLabel.Visible = false;
            errorTextBox.Visible = false;

            stopButton.Enabled = false;

            InitializePatternsTab();
            InitializeMasterList();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _network = new HTSNetwork();
            _network.SceneChangeHandler += HandleSceneChange;

            InitSignalGraph();
            EnumerateMATLABFunctions();

            UpdateProjectAndSubject(TdpAppSettings.LastProjectSubject);
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            await StartLogging();

            Log.Information($"TDP v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} started");

            Log.Information("Starting network...");
            _network.RemoteMessageHandler += HandleRemoteMessage;
            _network.ConnectionChanged += HandleConnectionChanged;

            _network.Initialize(this);  // start listener last  

            connectionStatusLabel.Image = imageList.Images[0];
            connectionStatusLabel.Text = "No tablet connection yet";

            matlabStatusLabel.Text = "Connecting...";
            matlabStatusLabel.Visible = true;
            var haveMATLAB = await MATLAB.Initialize();
            matlabStatusLabel.Visible = haveMATLAB;
            matlabStatusLabel.Text = "Available";
            if (haveMATLAB)
            {
                MATLAB.AddPath(FileLocations.MatlabFolder);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            TdpAppSettings.LastPosition = new Rectangle(Location, Size);

            MATLAB.CleanUp();

            if (!e.Cancel)
            {
                _network.Shutdown();
            }

            Log.Information("Exit");
            Log.CloseAndFlush();
        }

        private void RestoreLastPosition()
        {
            if (!TdpAppSettings.LastPosition.IsEmpty)
            {
                // Validate that the saved position is still visible on screen
                Rectangle savedBounds = TdpAppSettings.LastPosition;
                bool isVisible = false;

                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.IntersectsWith(savedBounds))
                    {
                        isVisible = true;
                        break;
                    }
                }

                if (isVisible)
                {
                    StartPosition = FormStartPosition.Manual;
                    Location = new Point(savedBounds.X, savedBounds.Y);
                    //Width = savedBounds.Width;
                    //Height = savedBounds.Height;
                }
                else
                {
                    // Position is off-screen, use default positioning
                    StartPosition = FormStartPosition.CenterScreen;
                    // Optionally clear the invalid position
                    TdpAppSettings.LastPosition = Rectangle.Empty;
                }
            }
        }

        private async Task StartLogging()
        {
            _logPath = Path.Combine(
                FileLocations.TdpFolder,
                "Logs",
                $"Tdp-{DateTime.Now.ToString("yyyyMMdd")}.txt");

            await Task.Run(() =>
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File(path: Path.Combine(_logPath),
                              retainedFileCountLimit: 30,
                              flushToDiskInterval: TimeSpan.FromSeconds(5),
                              buffered: true)
                .CreateLogger()
                );

            var listener = new SerilogTraceListener.SerilogTraceListener();
            Trace.Listeners.Add(listener);
        }

        private void InitSignalGraph()
        {
            // Hide axis label and tick
            signalGraph.Plot.Axes.Left.TickLabelStyle.IsVisible = false;
            signalGraph.Plot.Axes.Left.MajorTickStyle.Length = 0;
            signalGraph.Plot.Axes.Left.MinorTickStyle.Length = 0;
            signalGraph.Plot.XLabel("Time (s)");

            // Hide axis edge line
            signalGraph.Plot.Axes.Left.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Right.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Top.FrameLineStyle.Width = 0;
            signalGraph.Plot.Axes.Bottom.MinorTickStyle.Length = 0;

            signalGraph.Plot.Axes.Bottom.Label.Bold = false;
            signalGraph.Plot.Axes.Bottom.Label.FontSize = 12;
            signalGraph.Plot.Axes.Bottom.TickLabelStyle.FontSize = 12;

            signalGraph.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;
            var padding = new PixelPadding(
                left: 0,
                right: 0,
                bottom: 50, // keep some bottom padding for x-axis labels
                top: 0);
            signalGraph.Plot.Layout.Fixed(padding);
            signalGraph.Refresh();
        }

        private void EnumerateMATLABFunctions()
        {
            var mFileNames = Directory.GetFiles(FileLocations.MatlabFolder, "*.m", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
            matlabFunctionDropDown.Items.Clear();
            matlabFunctionDropDown.Items.AddRange(mFileNames.ToArray());

            if (mFileNames.Count == 0)
                return;

            if (mFileNames.Contains(TdpAppSettings.LastMatlabFile))
            {
                matlabFunctionDropDown.SelectedItem = TdpAppSettings.LastMatlabFile;
            }
            else
            {
                matlabFunctionDropDown.SelectedIndex = 0;
                TdpAppSettings.LastMatlabFile = mFileNames[0];
            }
        }

        // -------------------------------------------------------------------------
        // Network handlers
        // -------------------------------------------------------------------------
        private async void HandleConnectionChanged(object sender, bool connected)
        {
            if (connected)
            {
                connectionStatusLabel.Image = imageList.Images[1];
                connectionStatusLabel.Text = $"Connected to {_network.TabletAddress} (V{_network.TabletVersion})";
                sceneNameLabel.Visible = true;
                sceneNameLabel.Text = $"Scene: {_network.CurrentScene}";
                _currentHtsScene = _network.CurrentScene;
                RequestProjectAndSubject();
            }
            else
            {
                Log.Information("Tablet connection lost");
                connectionStatusLabel.Image = imageList.Images[0];
                connectionStatusLabel.Text = "No HTS connection, retrying...";
                subjectStatusLabel.Visible = false;
                sceneNameLabel.Visible = false;
            }
        }

        private void HandleRemoteMessage(object sender, TcpMessage message)
        {
            var payload = message.GetPayload<RemoteMessagePayload>();

            switch (message.Command)
            {
                case "SubjectChanged":
                    var subjectInfo = message.GetPayload<SubjectMetricsPayload>();
                    var playerSubject = $"{subjectInfo.Project}/{subjectInfo.Subject}";
                    Debug.WriteLine($"Received SubjectChanged message: {playerSubject}");
                    UpdateProjectAndSubject(playerSubject);
                    break;
                case "Progress":
                    break;
                case "ReceiveData":
                    //var filePayload = JsonConvert.DeserializeObject<TextFilePayload>(payload.Data);
                    //if (filePayload.Destination == FileDestination.SubjectMetadata)
                    //{
                    //    string audiogramPath = Path.Combine(SharedFileLocations.SubjectMetaFolder, filePayload.Filename);
                    //    if (!Directory.Exists(SharedFileLocations.SubjectMetaFolder))
                    //    {
                    //        Directory.CreateDirectory(SharedFileLocations.SubjectMetaFolder);
                    //    }
                    //    File.WriteAllText(audiogramPath, filePayload.Content);
                    //    break;
                    //}
                    //string filePath = Path.Combine(SharedFileLocations.HtsSubjectDataFolder, filePayload.Filename);
                    //if (File.Exists(filePath))
                    //{
                    //    Log.Warning($"File {filePath} already exists, backing up. This shouldn't happen.");
                    //    File.Move(filePath, filePath + ".bak");
                    //}
                    //File.WriteAllText(filePath, filePayload.Content);
                    break;
                case "Status":
                    Log.Information($"Status update: {payload.Data}");
                    Invoke(new Action(() => logTextBox.AppendText($"- {payload.Data}{Environment.NewLine}")));
                    break;
                case "Error":
                    Invoke(new Action(() => { EndRun("Error", payload.Data); }));
                    break;
                case "Finished":
                    Invoke(new Action(() => { EndRun("Finished", payload.Data); }));
                    break;
            }
        }

        private void HandleSceneChange(object sender, string sceneName)
        {
            sceneNameLabel.Text = $"Scene: {sceneName}";
            _currentHtsScene = sceneName;
        }

        private void RequestProjectAndSubject()
        {
            try
            {
                var subjectInfo = _network.SendRequest<string>("GetSubjectInfo");
                UpdateProjectAndSubject(subjectInfo);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to get project and subject: {ex.Message}");
            }
        }

        private void UpdateProjectAndSubject(string projectSubject)
        {
            if (string.IsNullOrEmpty(projectSubject))
            {
                Log.Warning("Received empty project/subject string.");
                return;
            }

            var parts = projectSubject.Split('/');
            if (parts.Length != 2)
            {
                Log.Warning($"Invalid project/subject format: {projectSubject}");
                return;
            }

            TdpAppSettings.LastProjectSubject = projectSubject;

            var project = parts[0];
            var subject = parts[1];
            _subjectName = subject;

            Log.Information($"Updating project to '{project}' and subject to '{subject}'.");

            SharedFileLocations.SetHtsSubject(project, subject);
            subjectStatusLabel.Visible = true;
            subjectStatusLabel.Text = $"Subject: {project}/{subject}";

            CreateSessionContext();
            EnumerateConfigFiles();
            LoadConfigFile(TdpAppSettings.LastConfigFile);
            RefreshSessionList();
        }

        private void CreateSessionContext()
        {
            var adapterMap = RequestAdapterMap();
            SessionContext.Initialize(adapterMap);

            var transducer = RequestSubjectTransducer();
            SessionContext.SetTransducer(transducer);
        }

        private AdapterMap RequestAdapterMap()
        {
            if (_network.IsConnected)
            {
                try
                {
                    return _network.SendRequest<AdapterMap>("GetAdapterMap");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetAdapterMap failed: {ex.Message}");
                }
            }

            return AdapterMap.Default7point1Map("HD280");
        }

        private string RequestSubjectTransducer()
        {
            if (_network.IsConnected)
            {
                try
                {
                    var metadata = _network.SendRequest<SubjectMetadata>("GetSubjectMetadata");
                    return metadata.Transducer;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetSubjectMetadata failed: {ex.Message}");
                }
            }
            return "HD280";
        }

        // -------------------------------------------------------------------------
        // Elements panel
        // -------------------------------------------------------------------------
        private void EnumerateConfigFiles()
        {
            configFileDropDown.Items.Clear();

            var configFileList = SharedFileLocations.EnumerateConfigFiles("Tapping");
            if (configFileList.Count == 0)
            {
                TdpAppSettings.LastConfigFile = null;
                return;
            }

            configFileDropDown.Items.AddRange(configFileList.ToArray());

            if (string.IsNullOrEmpty(TdpAppSettings.LastConfigFile) || !configFileList.Contains(TdpAppSettings.LastConfigFile))
            {
                TdpAppSettings.LastConfigFile = configFileList[0];
            }

            configFileDropDown.SelectedIndexChanged -= configFileDropDown_SelectedIndexChanged;
            configFileDropDown.SelectedItem = TdpAppSettings.LastConfigFile;
            configFileDropDown.SelectedIndexChanged += configFileDropDown_SelectedIndexChanged;
        }

        private void configFileDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            TdpAppSettings.LastConfigFile = configFileDropDown.SelectedItem.ToString();
            LoadConfigFile(TdpAppSettings.LastConfigFile);
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            _currentConfig = new TappingConfiguration();
            propertyGrid.SelectedObject = _currentConfig;
            PlotSignals();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (_currentConfig != null)
            {
                var fn = SharedFileLocations.GetConfigFile("Tapping", _currentConfig.Name);
                Files.XmlSerialize((BasicMeasurementConfiguration)_currentConfig, fn);
                TdpAppSettings.LastConfigFile = _currentConfig.Name;
                EnumerateConfigFiles();
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_currentConfig != null)
            {
                var fn = SharedFileLocations.GetConfigFile("Tapping", _currentConfig.Name);
                File.Delete(fn);
                _currentConfig = null;
                propertyGrid.SelectedObject = null;
                EnumerateConfigFiles();
                LoadConfigFile(TdpAppSettings.LastConfigFile);
            }

        }

        private void elementsHelpButton_Click(object sender, EventArgs e)
        {
            MarkdownDialog.ShowMarkdownDialog(FileLocations.ElementsTabHelp);
        }

        private void LoadConfigFile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _currentConfig = null;
                propertyGrid.SelectedObject = null;
                signalGraph.Plot.Clear();
                signalGraph.Refresh();
                return;
            }

            var configPath = SharedFileLocations.GetConfigFile("Tapping", name);
            if (File.Exists(configPath))
            {
                _currentConfig = Files.XmlDeserialize<BasicMeasurementConfiguration>(configPath) as TappingConfiguration;
                _currentConfig.WireOwners();
                propertyGrid.SelectedObject = _currentConfig;
                propertyGrid.ExpandAllGridItems();

                PlotSignals();
            }
        }

        private void PlotSignals()
        {
            errorTextBox.Visible = false;
            errorTextBox.Text = "";

            SignalManager sigman = new SignalManager();
            sigman.Channels.Add(_currentConfig.StimulusA);
            sigman.Channels.Add(_currentConfig.StimulusB);

            string chanName = "";
            int npts = 0;
            double[] time;
            try
            {
                signalGraph.Plot.Clear();

                float maxWidth = 0;
                foreach (var channel in sigman.Channels)
                {
                    maxWidth = Math.Max(maxWidth, channel.Gate.Active ? channel.Gate.Width_ms : 0);
                }
                float T = 0.001f * Math.Max(2 * maxWidth, 100);
                T = Math.Min(T, 25);

                npts = (int)(_plotSampleRate * T);

                sigman.Initialize(_plotSampleRate, npts, SessionContext.Signal);
                //channelView.UpdateMaxLevel();

                time = new double[npts];
            }
            catch (Exception ex)
            {
                errorTextBox.Text = ex.Message;
                errorTextBox.Visible = true;
                signalGraph.Refresh();
                return;
            }

            int irow = 0;
            foreach (KLib.Signals.Channel ch in sigman.Channels)
            {
                try
                {
                    chanName = ch.Name;
                    ch.Create();

                    double[] y = new double[npts];
                    var maxVal = ch.Data.Max();
                    double scaleFactor = maxVal > 0 ? 1 / ch.Data.Max() : 1;

                    for (int k = 0; k < npts; k++)
                    {
                        time[k] = k / _plotSampleRate;
                        y[k] = ch.Data[k] * scaleFactor + 2 * irow;
                    }

                    signalGraph.Plot.Add.SignalXY(time, y);
                }
                catch (Exception ex)
                {
                    errorTextBox.Text += chanName + ": " + ex.Message + Environment.NewLine;
                    errorTextBox.Visible = true;
                }
                --irow;
            }

            signalGraph.Plot.Axes.AutoScale();
            signalGraph.Refresh();
        }

        private static readonly HashSet<string> _changeTriggers = new HashSet<string>()
        {
            "Gate",
            "Bursted",
            "BurstRate",
            "Modulation",
            "Modality"
        };

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_changeTriggers.Contains(e.ChangedItem.Label))
            {
                propertyGrid.Refresh();
                propertyGrid.ExpandAllGridItems();
            }
            PlotSignals();
        }

        // -------------------------------------------------------------------------
        // Patterns panel
        // -------------------------------------------------------------------------
        private async void RunButton_Click(object sender, EventArgs e) => await ReplaySelectedAsync();
        private async void PreviewButton_Click(object sender, EventArgs e) => await PreviewSelectedAsync();

        /// <summary>
        /// Starts a tapping run on the HTS for the current config: switch to the Tapping
        /// scene, Initialize, Begin. Returns true if the run started; false on any failure
        /// (reason logged, run button re-enabled). Does NOT wait for the run to finish —
        /// completion still arrives via the Finished push message.
        /// </summary>
        private async Task<bool> StartTappingRunAsync(string arguments = "")
        {
            runButton.Enabled = false;
            dataPathTextBox.Text = "";
            logTextBox.Clear();
            _runStarted = false;
            _endRunStarted = false;

            var success = await ChangeRemoteScene("Tapping");
            if (!success)
            {
                logTextBox.Text = "ERROR: Failed to change scene to Tapping on tablet.";
                Log.Warning("Failed to change scene to Tapping on tablet.");
                runButton.Enabled = true;
                return false;
            }

            var payload = new TappingConfigPayload
            {
                Configuration = _currentConfig,
                Arguments = arguments
            };

            var result = _network.SendXmlRequest<string>("Initialize", payload);
            _dataPath = result ?? "";
            if (string.IsNullOrEmpty(_dataPath) || _dataPath.StartsWith("error"))
            {
                logTextBox.Text = "ERROR: Failed to initialize Tapping scene on tablet.";
                Log.Warning("Failed to initialize Tapping scene on tablet.");

                if (_dataPath.StartsWith("error"))
                {
                    logTextBox.AppendText(Environment.NewLine + _dataPath);
                    Log.Warning($"Tablet error: {_dataPath}");
                }

                runButton.Enabled = true;
                return false;
            }

            dataPathTextBox.Text = Path.GetFileName(_dataPath);
            stopButton.Enabled = true;
            logTextBox.AppendText("OK" + Environment.NewLine);
            _network.SendMessage("Begin");
            _runStarted = true;
            return true;
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            Log.Information("User stopping measurement");

            stopButton.Enabled = false;
            _network.SendMessage("Abort");
        }

        private void matlabFunctionDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            TdpAppSettings.LastMatlabFile = matlabFunctionDropDown.SelectedItem.ToString();
        }

        private async Task<bool> ChangeRemoteScene(string sceneName)
        {
            bool success = false;
            _network.SendMessage("ChangeScene", sceneName);

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < 5)
            {
                await Task.Delay(200);
                if (_network.CurrentScene.Equals(sceneName))
                {
                    success = true;
                    break;
                }
            }

            return success;
        }

        private async void EndRun(string message, string status)
        {
            if (!_runStarted || _endRunStarted) return;
            _endRunStarted = true;

            Log.Information("Run ending");

            if (!string.IsNullOrEmpty(status))
            {
                logTextBox.AppendText($"{Environment.NewLine}{status}{Environment.NewLine}");
            }

            stopButton.Enabled = false;
            runButton.Enabled = true;

            if (message == "Error" || status.Contains("aborted"))
                return;

            AnalyzeData();
        }

        private void AnalyzeData()
        {
            if (string.IsNullOrEmpty(_dataPath))
                return;

            if (string.IsNullOrEmpty(TdpAppSettings.LastMatlabFile))
                return;

            string matlabFunction = TdpAppSettings.LastMatlabFile;
            logTextBox.AppendText($"Running MATLAB function '{matlabFunction}'..." + Environment.NewLine);

            string wavFilePath = Path.Combine(SharedFileLocations.HtsSubjectDataFolder, Path.GetFileNameWithoutExtension(_dataPath) + "-Trial001.wav");
            Log.Information($"Running MATLAB function '{matlabFunction}' on file '{wavFilePath}'");
            string result = MATLAB.RunFunction(matlabFunction, wavFilePath);
            logTextBox.AppendText($"MATLAB analysis result:{Environment.NewLine}{result}{Environment.NewLine}");
        }

        private void patternsHelpButton_Click(object sender, EventArgs e)
        {
            MarkdownDialog.ShowMarkdownDialog(FileLocations.PatternsTabHelp);
        }
    }
}
