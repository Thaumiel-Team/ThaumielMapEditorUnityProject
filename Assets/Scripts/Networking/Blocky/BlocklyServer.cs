using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Assets.Scripts.Networking.Blocky
{
    [InitializeOnLoad]
    public static class BlocklyServer
    {
        private const string PREF_PORT = "BlocklyServer.Port";
        private const string PREF_ENABLED = "BlocklyServer.Enabled";
        private const string PREF_PING = "BlocklyServer.PingInterval";

        public static int Port { get => EditorPrefs.GetInt(PREF_PORT, 7070); set => EditorPrefs.SetInt(PREF_PORT, value); }
        public static bool AutoStart { get => EditorPrefs.GetBool(PREF_ENABLED, true); set => EditorPrefs.SetBool(PREF_ENABLED, value); }
        public static float PingInterval { get => EditorPrefs.GetFloat(PREF_PING, 15f); set => EditorPrefs.SetFloat(PREF_PING, value); }

        public static bool IsRunning => _running;
        public static string ActiveTargetEvent { get; set; }
        public static bool IsClientConnected => _client != null && _client.IsOpen;

        public static event Action<CodeExportPayload, string> OnCodeExport;
        public static event Action<string> OnXmlExport;
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;

        private static readonly ISerializer _ser = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();

        private static readonly IDeserializer _des = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static TcpListener _listener;
        private static WsConnection _client;
        private static Thread _acceptThread;
        private static bool _running;
        private static double _nextPingTime;

        private static readonly Queue<Action> _mainQueue = new();
        private static readonly object _queueLock = new();

        static BlocklyServer()
        {
            EditorApplication.update += EditorUpdate;
            EditorApplication.quitting += StopServer;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;

            if (AutoStart)
                StartServer();
        }

        private static void EditorUpdate()
        {
            lock (_queueLock)
            {
                while (_mainQueue.Count > 0)
                {
                    _mainQueue.Dequeue()?.Invoke();
                }
            }

            if (_running && IsClientConnected && PingInterval > 0f)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now >= _nextPingTime)
                {
                    _nextPingTime = now + PingInterval;
                    Send(new Dictionary<string, object>
                    {
                        ["type"] = "ping",
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    });
                }
            }
        }

        public static void StartServer()
        {
            if (_running)
                return;

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();
                _running = true;
                _nextPingTime = EditorApplication.timeSinceStartup + PingInterval;

                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "BlocklyServer-Accept"
                };
                _acceptThread.Start();
                Debug.Log($"[BlocklyServer] Server started on ws://localhost:{Port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlocklyServer] Failed to start: {ex.Message}");
            }
        }

        public static void StopServer()
        {
            if (!_running)
                return;

            _running = false;

            _client?.Close();
            _client = null;

            try
            {
                _listener?.Stop();
            }
            catch { }
            _listener = null;

            Debug.Log("[BlocklyServer] Server stopped.");
        }

        public static void RestartServer()
        {
            StopServer();
            EditorApplication.delayCall += StartServer;
        }

        private static void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient tcp = _listener.AcceptTcpClient();
                    _client?.Close();
                    _client = new WsConnection(tcp, OnRawMessage, OnConnectionClosed);
                    _client.Start();
                    Debug.Log("[BlocklyServer] Browser connected.");
                    Enqueue(() => OnClientConnected?.Invoke());
                }
                catch (SocketException) when (!_running) { break; }
                catch (Exception ex)
                {
                    if (_running)
                        Debug.LogWarning($"[BlocklyServer] Accept error: {ex.Message}");
                }
            }
        }

        private static void OnRawMessage(string yaml)
        {
            try
            {
                Dictionary<string, object> dict = _des.Deserialize<Dictionary<string, object>>(yaml);
                if (dict == null || !dict.TryGetValue("type", out var typObj))
                    return;

                string type = typObj?.ToString() ?? "";
                switch (type)
                {
                    case "connected":
                        Debug.Log("[BlocklyServer] Handshake received from browser.");
                        break;

                    case "code_export":
                        CodeExportPayload payload = _des.Deserialize<CodeExportPayload>(yaml);
                        Debug.Log($"[BlocklyServer] Code export ({payload.Language}).");
                        Enqueue(() => OnCodeExport?.Invoke(payload, ActiveTargetEvent));
                        break;

                    case "xml_export":
                        dict.TryGetValue("xml", out var xmlObj);
                        string xml = xmlObj?.ToString() ?? "";
                        Enqueue(() => OnXmlExport?.Invoke(xml));
                        break;

                    case "pong":
                        break; // heartbeat reply

                    case "block_registered":
                        dict.TryGetValue("id", out var bId);
                        Debug.Log($"[BlocklyServer] Block registered: {bId}");
                        break;

                    case "category_registered":
                        dict.TryGetValue("name", out var cName);
                        Debug.Log($"[BlocklyServer] Category registered: {cName}");
                        break;

                    default:
                        Debug.Log($"[BlocklyServer] Unknown msg type: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlocklyServer] YAML parse error: {ex.Message}");
            }
        }

        private static void OnConnectionClosed()
        {
            Enqueue(() =>
            {
                Debug.Log("[BlocklyServer] Browser disconnected.");
                OnClientDisconnected?.Invoke();
            });
        }

        /// <summary>Register a toolbox category in the browser.</summary>
        /// <param name="name">Display name.</param>
        /// <param name="color">Hue 0-360 or hex string e.g. "#FF6600".</param>
        /// <param name="icon">Emoji shown before the category name.</param>
        public static void RegisterCategory(string name, object color = null, string icon = "🔌")
        {
            Send(new Dictionary<string, object>
            {
                ["type"] = "register_category",
                ["name"] = name,
                ["color"] = color ?? 200,
                ["icon"] = icon
            });
        }

        /// <summary>
        /// Register a custom block in the browser toolbox.
        /// </summary>
        public static void RegisterBlock(BlockDefinition block)
        {
            Dictionary<string, object> dict = new()
            {
                ["type"] = "register_block",
                ["id"] = block.Id,
                ["message"] = block.Message,
                ["color"] = block.Color,
                ["tooltip"] = block.Tooltip ?? "",
            };

            if (block.Category != null)
                dict["category"] = block.Category;

            if (block.Args != null)
                dict["args"] = block.Args;

            if (block.Connections != null) 
                dict["connections"] = block.Connections;

            if (block.Generator != null)
                dict["generator"] = block.Generator;

            if (block.HelpUrl != null)
                dict["helpUrl"] = block.HelpUrl;

            Send(dict);
        }

        /// <summary>
        /// Ask the browser to send its current code immediately.
        /// </summary>
        public static void RequestExport() =>
            Send(new Dictionary<string, object> { ["type"] = "request_export" });

        /// <summary>
        /// Load a Blockly XML string into the browser workspace.
        /// </summary>
        public static void LoadXml(string xml) =>
            Send(new Dictionary<string, object> { ["type"] = "load_xml", ["xml"] = xml });

        /// <summary>
        /// Wipe the browser workspace.
        /// </summary>
        public static void ClearWorkspace() =>
            Send(new Dictionary<string, object> { ["type"] = "clear_workspace" });

        private static void Send(object data)
        {
            if (_client == null || !_client.IsOpen)
            {
                Debug.LogWarning("[BlocklyServer] No client connected message dropped.");
                return;
            }
            _client.SendText(_ser.Serialize(data));
        }

        private static void Enqueue(Action action)
        {
            lock (_queueLock)
            {
                _mainQueue.Enqueue(action);
            }
        }
    }
}