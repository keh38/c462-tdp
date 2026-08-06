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

namespace TapDevPlatform
{
    public partial class MainForm : Form
    {
        private HTSNetwork _network;

        private string _logPath;
        private string _currentHtsScene;

        private TappingConfiguration _currentConfig;
        private float _plotSampleRate = 48000;

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

            StopButton.Enabled = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _network = new HTSNetwork();
            _network.SceneChangeHandler += HandleSceneChange;

            InitSignalGraph();
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
            //if (haveMATLAB && !string.IsNullOrEmpty(subjectPageControl.Subject))
            //{
            //    MATLAB.AddPath(FileLocations.GetMATLABFolder(""));
            //}
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
                    Width = savedBounds.Width;
                    Height = savedBounds.Height;
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
            var project = parts[0];
            var subject = parts[1];
            Log.Information($"Updating project to '{project}' and subject to '{subject}'.");

            SharedFileLocations.SetHtsSubject(project, subject);
            subjectStatusLabel.Visible = true;
            subjectStatusLabel.Text = $"Subject: {project}/{subject}";

            CreateSessionContext();
            EnumerateConfigFiles();
            LoadConfigFile(TdpAppSettings.LastConfigFile);
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

                float T = 0.001f * sigman.GetMaxInterval(1000);
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
        private async void RunButton_Click(object sender, EventArgs e)
        {
            RunButton.Enabled = false;
            if (_currentHtsScene != "Tapping")
            {
                var success = await ChangeRemoteScene("Tapping");
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {

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

    }
}
