using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Serilog;

using C462.Shared.Protocol.DTOs;
using KLib.Net;
using HTS.Tcp;

namespace HTSController
{
    public class HTSNetwork
    {
        // -------------------------------------------------------------------------
        // Public events
        // -------------------------------------------------------------------------

        /// <summary>
        /// Raised when the connection to the HTS is established or lost.
        /// true = connected, false = disconnected.
        /// Always marshalled to the UI thread.
        /// </summary>
        public event EventHandler<bool> ConnectionChanged;

        /// <summary>
        /// Raised when the HTS sends an unsolicited TCP message to this controller.
        /// </summary>
        public event EventHandler<TcpMessage> RemoteMessageHandler;

        public event EventHandler<string> SceneChangeHandler;

        private void OnConnectionChanged(bool connected) => ConnectionChanged?.Invoke(this, connected);
        private void OnRemoteMessage(TcpMessage message) => RemoteMessageHandler?.Invoke(this, message);

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------

        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _myEndPoint;
        private string _hostName = "";
        private string _remoteVersionNumber = "";

        private DiscoveryListener _discoveryListener;
        private CancellationTokenSource _serverCancellationToken;
        private Control _uiControl;

        // -------------------------------------------------------------------------
        // Public properties
        // -------------------------------------------------------------------------

        public bool IsConnected => _remoteEndPoint != null;
        public string CurrentScene { get; private set; }
        public string TabletAddress => _remoteEndPoint?.ToString() ?? "";
        public string MyAddress => _myEndPoint?.ToString() ?? "";
        public string TabletVersion => _remoteVersionNumber;
        public IPEndPoint RemoteEndPoint => _remoteEndPoint;
        public DiscoveryListener DiscoveryListener => _discoveryListener;

        // -------------------------------------------------------------------------
        // Initialisation
        // -------------------------------------------------------------------------

        public HTSNetwork() 
        {
            _discoveryListener = new DiscoveryListener();
        }

        /// <summary>
        /// Starts the TCP listener and UDP discovery listener.
        /// Call once at application startup.
        /// </summary>
        /// <param name="uiControl">Any UI control — used to marshal events to the UI thread.</param>
        public void Initialize(Control uiControl)
        {
            _uiControl = uiControl;

            StartListener();

            _discoveryListener.HostDiscovered += OnHostDiscovered;
            _discoveryListener.HostDisappeared += OnHostDisappeared;
            _discoveryListener.Start();

            Log.Information("HTSNetwork initialized — listening for HTS beacon");
        }

        // -------------------------------------------------------------------------
        // Discovery handlers
        // -------------------------------------------------------------------------

        private void OnHostDiscovered(object sender, ServerBeacon beacon)
        {
            if (!beacon.Name.Equals("HEARING.TEST.SUITE", StringComparison.OrdinalIgnoreCase))
                return;

            if (_remoteEndPoint != null)
                return; // already connected

            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(beacon.Address), beacon.TcpPort);
                Log.Information($"HTS beacon received — contacting {endpoint}");

                var payload = new ConnectionRequestPayload
                {
                    Address = _myEndPoint.Address.ToString(),
                    Port = _myEndPoint.Port
                };

                var response = KTcpClient.SendRequest(endpoint, TcpMessage.Request("Connect", payload));

                if (!response.IsOk)
                {
                    Log.Information($"HTS refused connection — code {response.Code}: {response.Command}");
                    return;
                }

                var data = response.GetPayload<ConnectionResponsePayload>();
                _remoteEndPoint = endpoint;
                _hostName = data.HostName;
                _remoteVersionNumber = data.VersionNumber;
                CurrentScene = data.SceneName;

                Log.Information($"Connected to {_hostName} at {_remoteEndPoint} — scene: {CurrentScene}, version: {_remoteVersionNumber}");

                InvokeOnUI(() => OnConnectionChanged(true));
            }
            catch (Exception ex)
            {
                Log.Error($"Error connecting to HTS: {ex.Message}");
            }
        }

        private void OnHostDisappeared(object sender, ServerBeacon beacon)
        {
            if (!beacon.Name.Equals("HEARING.TEST.SUITE", StringComparison.OrdinalIgnoreCase))
                return;

            if (_remoteEndPoint == null)
                return; // already disconnected

            Log.Information("HTS beacon lost — connection dropped");
            ResetConnection();
            InvokeOnUI(() => OnConnectionChanged(false));
        }

        // -------------------------------------------------------------------------
        // Shutdown
        // -------------------------------------------------------------------------

        /// <summary>
        /// Notifies the HTS that this controller is shutting down, then stops all listeners.
        /// Call once from MainForm.OnFormClosing.
        /// </summary>
        public void Shutdown()
        {
            _discoveryListener?.Stop();

            if (_remoteEndPoint != null)
            {
                try
                {
                    KTcpClient.SendRequest(_remoteEndPoint, TcpMessage.Request("Disconnect"));
                    Log.Information("Disconnect sent to HTS");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not send Disconnect to HTS: {ex.Message}");
                }
            }

            _serverCancellationToken?.Cancel();
            ResetConnection();
        }

        // -------------------------------------------------------------------------
        // Outgoing messaging
        // -------------------------------------------------------------------------

        /// <summary>Sends a command with no payload. Returns true if the server responded OK.</summary>
        public bool SendMessage(string command)
        {
            if (_remoteEndPoint == null) return false;
            return KTcpClient.SendRequest(_remoteEndPoint, TcpMessage.Request(command)).IsOk;
        }

        /// <summary>Sends a command with a payload object. Returns true if the server responded OK.</summary>
        public bool SendMessage(string command, object payload)
        {
            if (_remoteEndPoint == null) return false;
            return KTcpClient.SendRequest(_remoteEndPoint, TcpMessage.Request(command, payload)).IsOk;
        }

        /// <summary>Sends a command with a payload object. Returns true if the server responded OK.</summary>
        public bool SendXmlMessage(string command, object payload)
        {
            if (_remoteEndPoint == null) return false;
            return KTcpClient.SendRequest(_remoteEndPoint, TcpMessage.XmlRequest(command, payload)).IsOk;
        }

        /// <summary>Sends a request and returns the typed payload from the response.</summary>
        public T SendRequest<T>(string command, object payload = null)
        {
            if (_remoteEndPoint == null) return default;
            var request = payload != null
                ? TcpMessage.Request(command, payload)
                : TcpMessage.Request(command);
            var response = KTcpClient.SendRequest(_remoteEndPoint, request);
            return response.IsOk ? response.GetPayload<T>() : default;
        }

        /// <summary>Sends a request and returns the typed payload from the response.</summary>
        public T SendXmlRequest<T>(string command, object payload = null)
        {
            if (_remoteEndPoint == null) return default;
            var request = payload != null
                ? TcpMessage.XmlRequest(command, payload)
                : TcpMessage.Request(command);
            var response = KTcpClient.SendRequest(_remoteEndPoint, request);
            return response.IsOk ? response.GetPayload<T>() : default;
        }

        public async Task<bool> SendBufferedFile(string localPath, string remoteFilename, FileDestination destination, string subPath="")
        {
            if (_remoteEndPoint == null) return false;

            int bufferSize = 16384;
            var fileInfo = new FileInfo(localPath);
            long numBuffers = (long)Math.Ceiling((double)fileInfo.Length / bufferSize);

            return await Task.Run(() =>
            {
                var client = new KTcpClient();
                try
                {
                    client.StartBufferedSend(_remoteEndPoint);

                    var payload = new BufferedFilePayload
                    {
                        Destination = destination,
                        SubPath = subPath,
                        Filename = remoteFilename,
                        NumBuffers = numBuffers,
                        BufferSize = bufferSize
                    };

                    // Send control message using same wire format as normal TcpMessage exchange
                    client.WriteBuffer(Encoding.UTF8.GetBytes(
                        TcpMessage.Request("ReceiveBufferedFile", payload).Serialize()));

                    var ready = client.ReadBufferedSendResponse();
                    if (!ready.IsOk)
                    {
                        Log.Warning($"ReceiveBufferedFile refused: {ready.Command}");
                        return false;
                    }

                    using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(fs))
                    {
                        for (long k = 0; k < numBuffers; k++)
                        {
                            var bytes = reader.ReadBytes(bufferSize);
                            client.WriteBuffer(bytes);
                        }
                    }

                    var complete = client.ReadBufferedSendResponse();
                    Log.Information($"SendBufferedFile complete: {remoteFilename}");
                    return complete.IsOk;
                }
                catch (Exception ex)
                {
                    Log.Error($"SendBufferedFile failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    client.EndBufferedSend();
                }
            });
        }
        // -------------------------------------------------------------------------
        // TCP listener — receives unsolicited messages from HTS
        // Phase 2: ProcessTCPMessage will be migrated to TcpMessage protocol
        // -------------------------------------------------------------------------

        private void StartListener()
        {
            _myEndPoint = Discovery.FindNextAvailableEndPoint();

            _serverCancellationToken = new CancellationTokenSource();
            Task.Run(() =>
            {
                Listener(_myEndPoint, _serverCancellationToken.Token);
            }, _serverCancellationToken.Token);
        }

        private void Listener(IPEndPoint endpoint, CancellationToken ct)
        {
            var server = new KTcpListener();
            server.StartListener(endpoint);
            Log.Information($"TCP server started on {endpoint}");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (server.Pending())
                        ProcessTCPMessage(server);
                }
                catch (Exception ex)
                {
                    Log.Information(ex.Message);
                }
                Thread.Sleep(10);
            }

            server.CloseListener();
            Log.Information("TCP server stopped");
        }

        private void ProcessTCPMessage(KTcpListener server)
        {
            server.AcceptTcpClient();
            var request = server.ReadRequest();

            switch (request.Command)
            {
                case "ChangedScene":
                    var sceneData = request.GetPayload<ChangeScenePayload>();
                    server.WriteResponse(TcpMessage.Ok());
                    CurrentScene = sceneData.SceneName;
                    Log.Information($"HTS reports scene changed to {CurrentScene}");
                    SceneChangeHandler?.Invoke(this, CurrentScene);
                    break;

                case "Disconnect":
                    server.WriteResponse(TcpMessage.Ok());
                    Log.Information($"HTS remotely disconnected");
                    var address = _remoteEndPoint?.Address.ToString();
                    ResetConnection();
                    if (address != null)
                        _discoveryListener.ForgetHost("HEARING.TEST.SUITE");
                    InvokeOnUI(() => OnConnectionChanged(false));
                    break;

                case "ReceiveBufferedFile":
                    //var filePayload = request.GetPayload<BufferedFilePayload>();
                    //server.WriteResponse(TcpMessage.Ok()); // signal ready

                    //var destPath = Path.Combine("figgeridoot", filePayload.Filename);
                    //Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                    //using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    //using (var writer = new BinaryWriter(fs))
                    //{
                    //    for (long k = 0; k < filePayload.NumBuffers; k++)
                    //    {
                    //        //var bytes = server.ReadRawBytes();
                    //        //writer.Write(bytes);
                    //    }
                    //}

                    //server.WriteResponse(TcpMessage.Ok()); // signal complete
                    //Log.Information($"ReceiveFile complete: {filePayload.Filename}");
                    break;

                default:
                    server.WriteResponse(TcpMessage.Ok());
                    if (request.Payload == "{}")
                    {
                        // Legacy HTS string format: "Target:Command:Data"
                        var msgParts = request.Command.Split(new char[] { ':' }, 3);
                        var remoteCmd = msgParts.Length > 1 ? msgParts[1] : msgParts[0];
                        var remoteTarget = msgParts.Length > 1 ? msgParts[0] : "";
                        var remoteData = msgParts.Length > 2 ? msgParts[2] : "";
                        OnRemoteMessage(TcpMessage.Request(remoteCmd, new RemoteMessagePayload { Target = remoteTarget, Data = remoteData }));
                    }
                    else
                    {
                        // New style: Command is the command, Payload contains RemoteMessagePayload
                        OnRemoteMessage(request);
                    }
                    break;
            }

            server.CloseTcpClient();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void ResetConnection()
        {
            _remoteEndPoint = null;
            _hostName = "";
            CurrentScene = null;
            _remoteVersionNumber = "";
        }

        private void InvokeOnUI(Action action)
        {
            if (_uiControl != null && _uiControl.InvokeRequired)
                _uiControl.Invoke(action);
            else
                action();
        }
    }
}
