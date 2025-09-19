using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks; // Added
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.API.Interfaces;
using revit_mcp_plugin.Configuration;
using revit_mcp_plugin.Utils;

namespace revit_mcp_plugin.Core
{
    public class SocketService
    {
        private static SocketService _instance;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private bool _isInitialized = false;
        private int _port = 8080;
        // This is not used in the original code after initialization, so I can remove it.
        // private UIApplication _uiApp;
        private ICommandRegistry _commandRegistry;
        private ILogger _logger;
        private CommandExecutor _commandExecutor;

        public static SocketService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new SocketService();
                return _instance;
            }
        }

        private SocketService()
        {
            _commandRegistry = new RevitCommandRegistry();
            _logger = new Logger();
        }

        public bool IsRunning => _isRunning;
        public bool IsInitialized => _isInitialized;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        // 初始化
        public void Initialize(UIApplication uiApp)
        {
            if (_isInitialized) return;

            // _uiApp = uiApp; // No longer needed to be stored

            // 初始化事件管理器
            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            // 记录当前 Revit 版本
            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("当前 Revit 版本: {0}", currentVersion);

            // 创建命令执行器
            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // 加载配置并注册命令
            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();
            
            _port = 8080; // 固定端口号

            // 加载命令
            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, uiApp);
            commandManager.LoadCommands();

            _logger.Info($"Socket service initialized on port {_port}");
            _isInitialized = true;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();              
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start socket listener: " + ex.Message);
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                _listener?.Stop();
                _listener = null;

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error while stopping socket listener: " + ex.Message);
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    // Each client connection is handled on its own thread. This means that a long-running,
                    // blocking command from one client will not prevent the server from accepting new
                    // connections from other clients. However, the connection for the blocked client
                    // will be unresponsive until the command completes.
                    Thread clientThread = new Thread(async (c) => await HandleClientCommunication(c))
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                }
            }
            catch (SocketException)
            {
                // Listener was stopped. This is expected.
            }
            catch(Exception ex)
            {
                _logger.Error("Error in ListenForClients: " + ex.Message);
            }
        }

        private async Task HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    }
                    catch (IOException)
                    {
                        break; // Client disconnected
                    }

                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _logger.Info($"收到消息: {message}");

                    string response = await ProcessJsonRPCRequestAsync(message);

                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData, 0, responseData.Length);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Error in HandleClientCommunication: " + ex.Message);
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private async Task<string> ProcessJsonRPCRequestAsync(string requestJson)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                if (request == null || !request.IsValid())
                {
                    return CreateErrorResponse(null, JsonRPCErrorCodes.InvalidRequest, "Invalid JSON-RPC request");
                }

                // Use the CommandExecutor to handle the request asynchronously
                return await _commandExecutor.ExecuteCommandAsync(request);
            }
            catch (JsonException)
            {
                return CreateErrorResponse(null, JsonRPCErrorCodes.ParseError, "Invalid JSON");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(null, JsonRPCErrorCodes.InternalError, $"Internal error: {ex.Message}");
            }
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }
    }
}
