using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Networking.Blocky
{
    public class BlocklyServerWindow : EditorWindow
    {

        [MenuItem("Thaumiel/Tools/Blockly Server")]
        public static void Open() => GetWindow<BlocklyServerWindow>("Blockly Server");


        private static readonly List<(string time, string kind, string msg)> _log = new(200);
        private static readonly object _logLock = new();
        private Vector2 _scroll;

        private static readonly Color ColSend = new(0.30f, 0.67f, 0.97f);
        private static readonly Color ColRecv = new(0.39f, 0.88f, 0.42f);
        private static readonly Color ColErr = new(1.00f, 0.42f, 0.42f);
        private static readonly Color ColInfo = new(0.55f, 0.55f, 0.65f);

        private string _portBuf = "";
        private bool _portBufInit;

        private static bool _hooked;

        private void OnEnable()
        {
            if (!_hooked)
            {
                _hooked = true;
                BlocklyServer.OnClientConnected += () => AddLog("info", "Browser connected");
                BlocklyServer.OnClientDisconnected += () => AddLog("info", "Browser disconnected");
                BlocklyServer.OnCodeExport += (p, s) => AddLog("recv", $"code_export ({p.Code?.Length ?? 0} chars)");
                BlocklyServer.OnXmlExport += _ => AddLog("recv", "xml_export received");
            }

            if (!_portBufInit)
            {
                _portBuf = BlocklyServer.Port.ToString();
                _portBufInit = true;
            }

            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            DrawStatusBar();
            EditorGUILayout.Space(4);
            DrawSettings();
            EditorGUILayout.Space(4);
            DrawActions();
            EditorGUILayout.Space(4);
            DrawLog();
        }

        private void DrawStatusBar()
        {
            bool running = BlocklyServer.IsRunning;
            bool client = BlocklyServer.IsClientConnected;

            Color dot = running ? (client ? new Color(0.39f, 0.88f, 0.42f) : new Color(1.00f, 0.76f, 0.03f)) : new Color(0.55f, 0.55f, 0.65f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                Color prev = GUI.color;
                GUI.color = dot;
                GUILayout.Label("●", EditorStyles.boldLabel, GUILayout.Width(18));
                GUI.color = prev;

                string label = running ? (client ? $"Connected  |  ws://localhost:{BlocklyServer.Port}" : $"Listening  |  ws://localhost:{BlocklyServer.Port}") : "Stopped";

                GUILayout.Label(label, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                if (running)
                {
                    if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(50)))
                        BlocklyServer.StopServer();

                    if (GUILayout.Button("Restart", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        BlocklyServer.RestartServer();
                }
                else
                {
                    if (GUILayout.Button("Start", EditorStyles.toolbarButton, GUILayout.Width(50)))
                        BlocklyServer.StartServer();
                }
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                bool autoStart = EditorGUILayout.Toggle("Start on Unity Open", BlocklyServer.AutoStart);
                if (autoStart != BlocklyServer.AutoStart)
                    BlocklyServer.AutoStart = autoStart;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Port", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                    _portBuf = EditorGUILayout.TextField(_portBuf);

                    if (GUILayout.Button("Apply", GUILayout.Width(55)))
                    {
                        if (int.TryParse(_portBuf, out int p) && p > 1024 && p < 65536)
                        {
                            BlocklyServer.Port = p;
                            if (BlocklyServer.IsRunning)
                                BlocklyServer.RestartServer();
                        }
                        else
                        {
                            Debug.LogWarning("[BlocklyServer] Port must be between 1025 and 65535.");
                            _portBuf = BlocklyServer.Port.ToString();
                        }
                    }
                }

                float ping = EditorGUILayout.FloatField("Ping Interval (s)", BlocklyServer.PingInterval);
                if (!Mathf.Approximately(ping, BlocklyServer.PingInterval))
                    BlocklyServer.PingInterval = Mathf.Max(0f, ping);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = BlocklyServer.IsClientConnected;

                if (GUILayout.Button("Request Export"))
                    BlocklyServer.RequestExport();

                if (GUILayout.Button("Clear Workspace"))
                    BlocklyServer.ClearWorkspace();

                GUI.enabled = true;
            }
        }

        private void DrawLog()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
                    lock (_logLock)
                    {
                        _log.Clear();
                    }
            }

            float lineH = EditorGUIUtility.singleLineHeight + 1;
            int count;
            lock (_logLock)
            {
                count = _log.Count;
            }

            float viewH = Mathf.Max(80, position.height - 220);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(viewH));

            lock (_logLock)
            {
                for (int i = 0; i < _log.Count; i++)
                {
                    var (time, kind, msg) = _log[i];
                    Color c = kind switch
                    {
                        "send" => ColSend,
                        "recv" => ColRecv,
                        "err" => ColErr,
                        _ => ColInfo
                    };
                    string prefix = kind switch
                    {
                        "send" => "→",
                        "recv" => "←",
                        "err" => "✖",
                        _ => "·"
                    };

                    Color prev = GUI.color;
                    GUI.color = c;
                    EditorGUILayout.LabelField($"{time}  {prefix}  {msg}",
                        EditorStyles.miniLabel, GUILayout.Height(lineH));
                    GUI.color = prev;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public static void AddLog(string kind, string msg)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            lock (_logLock)
            {
                _log.Add((time, kind, msg));
                if (_log.Count > 500)
                    _log.RemoveAt(0);
            }
        }
    }
}
