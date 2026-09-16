using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Components.Objects;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public class CollabWindow : EditorWindow
    {
        private string _hostIp = "127.0.0.1";
        private int _port = 7777;
        private string _userName = "editor";
        private Color _color = new(0.2f, 0.67f, 1f);
        private Vector2 _scroll;
        private readonly List<string> _log = new();
        private readonly bool _showHelp = true;

        [MenuItem("Thaumiel/Tools/Collab")]
        public static void Open()
        {
            GetWindow<CollabWindow>("Collab");
        }

        private void OnEnable()
        {
            _hostIp = CollabNet.HostIp;
            _port = CollabNet.Port;
            _userName = CollabNet.UserName;
            _color = CollabNet.UserColor;
            CollabNet.OnLog += OnNetLog;
            CollabNet.OnUsersChanged += OnUsersChanged;
        }

        private void OnDisable()
        {
            CollabNet.OnLog -= OnNetLog;
            CollabNet.OnUsersChanged -= OnUsersChanged;
        }

        private void OnNetLog(string s)
        {
            _log.Add($"[{DateTime.Now:HH:mm:ss}] {s}");
            if (_log.Count > 200)
                _log.RemoveAt(0);

            Repaint();
        }

        private void OnUsersChanged() => Repaint();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MultiUser Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Status: {CollabNet.Mode}" + (CollabNet.Mode == CollabMode.Client ? (CollabNet.Welcomed ? " (synced)" : " (joining…)") : string.Empty));

            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Host a session:\n" +
                    "- Pick a name + color, then click Apply.\n" +
                    "- Click Start Host, then send friends your address + port.\n" +
                    "- Same wifi: click Local IPs. Over internet: use a proxy service or forward the port in your router.\n" +
                    "- Allow friends under Join requests.\n\n" +
                    "Join a session:\n" +
                    "- Pick a name + color, then click Apply.\n" +
                    "- Paste the host address into Host, then Join.\n" +
                    "- Wait for the host to allow you - their map will load automatically.\n\n" +
                    "Tips: Everyone needs the same map files open. Don't press Play. " +
                    "If maps look different, ask the host to Push full map. Click Leave when done.\n",
                    MessageType.Info);
            }

            if (CollabNet.Mode == CollabMode.Off && !string.IsNullOrEmpty(CollabNet.LastDenyReason))
            {
                EditorGUILayout.HelpBox($"Join denied: {CollabNet.LastDenyReason}", MessageType.Error);
                if (GUILayout.Button("Dismiss"))
                    CollabNet.ClearDenyReason();
            }

            EditorGUILayout.Space(4);
            _userName = EditorGUILayout.TextField("Display name", _userName);
            _color = EditorGUILayout.ColorField("My color", _color);
            _port = EditorGUILayout.IntField("UDP port", _port);
            _hostIp = EditorGUILayout.TextField("Host (IP/hostname[:port])", _hostIp);

            bool wantApproval = EditorGUILayout.Toggle("Require approval to join (host)", CollabNet.RequireApproval);
            if (wantApproval != CollabNet.RequireApproval)
                CollabNet.RequireApproval = wantApproval;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply identity"))
            {
                CollabNet.UserName = _userName;
                CollabNet.UserColor = _color;
                if (CollabNet.TryParseHostInput(_hostIp, _port, out string parsedHost, out int parsedPort, out _))
                {
                    _hostIp = parsedHost;
                    _port = parsedPort;
                    CollabNet.HostIp = parsedHost;
                    CollabNet.Port = parsedPort;
                }
                else
                {
                    CollabNet.HostIp = _hostIp;
                    CollabNet.Port = _port;
                }
            }
            if (GUILayout.Button("Local IP?"))
            {
                try
                {
                    string host = Dns.GetHostName();
                    IPHostEntry entry = Dns.GetHostEntry(host);
                    string ips = string.Empty;
                    foreach (IPAddress a in entry.AddressList)
                    {
                        if (a.AddressFamily == AddressFamily.InterNetwork)
                            ips += a + "  ";
                    }
                    PushLog("Local IPs: " + ips);
                }
                catch (Exception ex)
                {
                    PushLog("IP lookup failed: " + ex.Message);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = CollabNet.Mode == CollabMode.Off;
            if (GUILayout.Button("Start Host", GUILayout.Height(28)))
            {
                CollabNet.UserName = _userName;
                CollabNet.UserColor = _color;
                if (CollabNet.StartHost(_port))
                {
                    EnsureIdsForSession();
                }
                else
                    PushLog("Host failed to bind. Port in use?");
            }
            if (GUILayout.Button("Join", GUILayout.Height(28)))
            {
                CollabNet.UserName = _userName;
                CollabNet.UserColor = _color;
                if (CollabNet.StartClient(_hostIp, _port))
                {
                    _hostIp = CollabNet.HostIp;
                    _port = CollabNet.Port;
                }
                else
                    PushLog("Join failed (bad host/DNS or socket).");
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = CollabNet.Mode != CollabMode.Off;
            if (GUILayout.Button("Push full snapshot (host)"))
            {
                if (CollabNet.Mode != CollabMode.Host)
                {
                    PushLog("Only host can push snapshots.");
                }
                else
                    PushSnapshotToAll();
            }
            if (GUILayout.Button("Leave", GUILayout.Height(24)))
            {
                CollabNet.Stop();
                PushLog("Left session.");
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Peers", EditorStyles.boldLabel);
            if (CollabNet.Mode == CollabMode.Host)
            {
                EditorGUILayout.LabelField("Join requests", EditorStyles.boldLabel);
                List<KeyValuePair<string, CollabClientInfo>> pending = new(CollabNet.PendingJoins);
                if (pending.Count == 0)
                    EditorGUILayout.LabelField(CollabNet.RequireApproval ? "(none)" : "(auto-allow on)");

                foreach (KeyValuePair<string, CollabClientInfo> kv in pending)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {kv.Value.name} [{kv.Key.Substring(0, Math.Min(6, kv.Key.Length))}] @ {kv.Value.endpoint}");
                    if (GUILayout.Button("Allow", GUILayout.Width(60)))
                        CollabNet.ApproveJoin(kv.Key);

                    if (GUILayout.Button("Deny", GUILayout.Width(60)))
                        CollabNet.DenyJoin(kv.Key);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Connected", EditorStyles.boldLabel);
                List<KeyValuePair<string, CollabClientInfo>> clients = new(CollabNet.Clients);
                foreach (KeyValuePair<string, CollabClientInfo> kv in clients)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {kv.Value.name} [{kv.Key.Substring(0, Math.Min(6, kv.Key.Length))}] @ {kv.Value.endpoint}");
                    if (GUILayout.Button("Kick", GUILayout.Width(60)))
                        CollabNet.KickClient(kv.Key);
                        
                    EditorGUILayout.EndHorizontal();
                }

                if (clients.Count == 0)
                    EditorGUILayout.LabelField("(waiting for joiners…)");
            }
            else
            {
                List<UserInfo> users = CollabNet.RemoteUsers;
                if (users.Count == 0)
                    EditorGUILayout.LabelField(CollabNet.Mode == CollabMode.Off ? "(offline)" : "(waiting for host presence…)");

                foreach (UserInfo u in users)
                {
                    EditorGUILayout.LabelField($"• {u.name}");
                }

                foreach (KeyValuePair<string, CollabApplier.RemotePeer> kv in CollabApplier.Peers)
                {
                    EditorGUILayout.LabelField($"• {kv.Value.name} (selecting)");
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
            foreach (string line in _log)
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        private void PushLog(string s)
        {
            _log.Add($"[{DateTime.Now:HH:mm:ss}] {s}");
            if (_log.Count > 200)
                _log.RemoveAt(0);
        }

        private static void EnsureIdsForSession()
        {
            Builder[] builders = FindObjectsByType<Builder>(FindObjectsSortMode.None);
            int n = 0;
            foreach (Builder b in builders)
            {
                if (b == null)
                    continue;

                CollabId.Ensure(b.gameObject);
                foreach (ObjectBase block in b.GetComponentsInChildren<ObjectBase>(true))
                {
                    if (block == null)
                        continue;

                    CollabId.Ensure(block.gameObject);
                    n++;
                }
            }

            CollabTracker.RebuildCache();
            Debug.Log($"[Collab] Session ids ensured ({n} objects).");
        }

        private static void PushSnapshotToAll()
        {
            CollabNet.PushSnapshotToAll();
        }
    }
}
