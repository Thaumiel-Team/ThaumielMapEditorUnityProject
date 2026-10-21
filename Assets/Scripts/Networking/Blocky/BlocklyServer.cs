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

        private static bool _batching;
        private static readonly List<Dictionary<string, object>> _batchedCategories = new();
        private static readonly List<Dictionary<string, object>> _batchedBlocks = new();
        private static readonly List<Dictionary<string, object>> _batchedEvents = new();

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
                        if (payload != null)
                            BlockyPayloadCodec.Normalize(payload);
                            
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

                    case "blocks_registered":
                        dict.TryGetValue("count", out var blkCount);
                        Debug.Log($"[BlocklyServer] Block batch registered: {blkCount} blocks.");
                        break;

                    case "category_registered":
                        dict.TryGetValue("name", out var cName);
                        Debug.Log($"[BlocklyServer] Category registered: {cName}");
                        break;

                    case "event_registered":
                        dict.TryGetValue("id", out var eId);
                        Debug.Log($"[BlocklyServer] Event registered: {eId}");
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
            Dictionary<string, object> dict = new()
            {
                ["type"] = "register_category",
                ["name"] = name,
                ["color"] = color ?? 200,
                ["icon"] = icon
            };

            if (_batching)
            {
                _batchedCategories.Add(dict);
            }
            else
                Send(dict);
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

            dict["connections"] = block.Connections.ToList();

            if (block.Generator != null)
                dict["generator"] = block.Generator;

            if (block.OutputType != null)
                dict["outputType"] = block.OutputType;

            if (block.PropertyTypes != null && block.PropertyTypes.Count > 0)
                dict["propertyTypes"] = new Dictionary<string, string>(block.PropertyTypes);

            if (block.HelpUrl != null)
                dict["helpUrl"] = block.HelpUrl;

            if (block.Explanation != null)
                dict["explanation"] = block.Explanation;

            if (_batching)
            {
                _batchedBlocks.Add(dict);
            }
            else
                Send(dict);
        }

        /// <summary>
        /// Report validation issues to the browser. They appear in its issue
        /// list, counters, and block badges. Replaces the previous set, so send
        /// the full current list each time. Blocks are matched by type via
        /// <see cref="BlockIssue.BlockTypes"/>; issues without types are shown
        /// workspace wide.
        ///
        /// Example:
        ///   BlocklyServer.ReportIssues(new[]
        ///   {
        ///       new BlockIssue { Id = "hp-range", Level = "warn",
        ///           Message = "Health above 100 needs a max-health block.",
        ///           BlockTypes = new List<string> { "set_player_health" } },
        ///   });
        /// </summary>
        public static void ReportIssues(IEnumerable<BlockIssue> issues)
        {
            List<Dictionary<string, object>> list = new();
            if (issues != null)
            {
                foreach (BlockIssue issue in issues)
                {
                    if (issue == null || string.IsNullOrEmpty(issue.Id) || string.IsNullOrEmpty(issue.Message))
                        continue;

                    Dictionary<string, object> dict = new()
                    {
                        ["id"] = issue.Id,
                        ["level"] = issue.Level ?? "warn",
                        ["message"] = issue.Message,
                    };

                    if (issue.BlockTypes != null && issue.BlockTypes.Count > 0)
                        dict["blockTypes"] = new List<string>(issue.BlockTypes);

                    list.Add(dict);
                }
            }

            Send(new Dictionary<string, object> { ["type"] = "register_issues", ["issues"] = list });
        }

        /// <summary>
        /// Clear all server reported issues in the browser.
        /// </summary>
        public static void ClearIssues() => ReportIssues(null);

        /// <summary>
        /// Register a conditional validation rule in the browser. The browser
        /// evaluates it live against every block and shows an issue wherever
        /// the condition matches. 
        /// Registering again with the same id replaces the rule.
        ///
        /// Example (boolean compared to anything else):
        ///   BlocklyServer.RegisterCheck(new BlockCheck
        ///   {
        ///       Id = "compare-mismatch", Level = "err",
        ///       Message = "Cannot compare {A} with {B}.",
        ///       Blocks = new List<string> { "logic_compare" },
        ///       InputTypesDiffer = new List<string> { "A", "B" },
        ///   });
        /// </summary>
        public static void RegisterCheck(BlockCheck check)
        {
            if (check == null || string.IsNullOrEmpty(check.Id) || string.IsNullOrEmpty(check.Message))
                return;

            Dictionary<string, object> dict = new()
            {
                ["type"] = "register_check",
                ["id"] = check.Id,
                ["level"] = check.Level ?? "warn",
                ["message"] = check.Message,
            };

            if (check.Blocks != null && check.Blocks.Count > 0)
                dict["blocks"] = new List<string>(check.Blocks);

            if (check.InputTypesDiffer != null && check.InputTypesDiffer.Count >= 2)
                dict["inputTypesDiffer"] = new List<string> { check.InputTypesDiffer[0], check.InputTypesDiffer[1] };

            if (check.MismatchInputs != null && check.MismatchInputs.Count > 0 && !string.IsNullOrEmpty(check.MismatchExpected))
            {
                dict["mismatchInputs"] = new List<string>(check.MismatchInputs);
                dict["mismatchExpected"] = check.MismatchExpected;
            }

            if (check.RequiredInputs != null && check.RequiredInputs.Count > 0)
                dict["requiredInputs"] = new List<string>(check.RequiredInputs);

            Send(dict);
        }

        /// <summary>
        /// Clear all server registered validation rules in the browser.
        /// </summary>
        public static void ClearChecks() =>
            Send(new Dictionary<string, object> { ["type"] = "clear_checks" });

        public static void BeginBatch()
        {
            _batching = true;
            _batchedCategories.Clear();
            _batchedBlocks.Clear();
            _batchedEvents.Clear();
        }

        public static void EndBatch()
        {
            _batching = false;

            if (_batchedCategories.Count == 0 && _batchedBlocks.Count == 0 && _batchedEvents.Count == 0)
                return;

            Dictionary<string, object> batch = new()
            {
                ["type"] = "register_batch",
                ["categories"] = _batchedCategories,
                ["blocks"] = _batchedBlocks
            };

            if (_batchedEvents.Count > 0)
                batch["events"] = _batchedEvents;

            Send(batch);

            _batchedCategories.Clear();
            _batchedBlocks.Clear();
            _batchedEvents.Clear();
        }

        /// <summary>
        /// Register a custom event block in the browser Events toolbox.
        /// Each param becomes a variable dropdown on the block and a
        /// same position parameter in the generated C# method.
        /// Sent immediately (never batched); call after the client connects.
        ///
        /// Example:
        ///   BlocklyServer.RegisterEvent(new BlockEvent
        ///   {
        ///       Id = "my_custom_event", Label = "OnCustom", Method = "OnCustom",
        ///       Params = new List&lt;BlockEventParam&gt;
        ///       {
        ///           BlockEventParam.Var("player", "Player"),
        ///           BlockEventParam.Var("amount", "int"),
        ///       },
        ///       Tooltip = "Runs when something custom happens.",
        ///   });
        /// </summary>
        public static void RegisterEvent(BlockEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.Id))
                return;

            Dictionary<string, object> dict = new()
            {
                ["type"] = "register_event",
                ["id"] = evt.Id,
                ["label"] = evt.Label ?? evt.Id,
                ["color"] = evt.Color ?? "#E65100",
                ["tooltip"] = evt.Tooltip ?? "",
                ["explanation"] = evt.Explanation ?? "",
            };

            if (!string.IsNullOrEmpty(evt.Method))
                dict["method"] = evt.Method;

            if (evt.Params != null && evt.Params.Count > 0)
            {
                List<Dictionary<string, object>> pars = new();
                foreach (BlockEventParam p in evt.Params)
                {
                    if (p == null || string.IsNullOrEmpty(p.Name))
                        continue;

                    pars.Add(new Dictionary<string, object>
                    {
                        ["name"] = p.Name,
                        ["type"] = p.Type ?? "object",
                    });
                }

                dict["params"] = pars;
            }

            if (_batching)
            {
                _batchedEvents.Add(dict);
            }
            else
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

        public static void LoadXml(string xml, string eventBlockType) =>
            Send(new Dictionary<string, object> { ["type"] = "load_xml", ["xml"] = xml, ["event"] = eventBlockType ?? "" });

        public static void LoadEvent(string eventBlockType) =>
            Send(new Dictionary<string, object> { ["type"] = "load_event", ["event"] = eventBlockType });

        public static void EnsureEvent(string eventBlockType) =>
            Send(new Dictionary<string, object> { ["type"] = "ensure_event", ["event"] = eventBlockType });

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