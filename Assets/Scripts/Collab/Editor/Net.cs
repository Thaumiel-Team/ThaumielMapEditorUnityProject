using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Collab;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public enum CollabMode
    {
        Off = 0,
        Host,
        Client
    }

    public class CollabClientInfo
    {
        public string userId = string.Empty;
        public string name = string.Empty;
        public string color = "#33AAFF";
        public IPEndPoint endpoint;
        public long lastSeenTicks;
    }

    [InitializeOnLoad]
    public static class CollabNet
    {
        private const string P_UserId = "CollabNet.UserId";
        private const string P_UserName = "CollabNet.UserName";
        private const string P_Color = "CollabNet.Color";
        private const string P_Port = "CollabNet.Port";
        private const string P_HostIp = "CollabNet.HostIp";
        private const string P_RequireApproval = "CollabNet.RequireApproval";

        public static CollabMode Mode { get; private set; } = CollabMode.Off;
        public static bool Connected => Mode != CollabMode.Off && _transport != null;
        public static bool Welcomed { get; private set; }

        public static string UserId
        {
            get
            {
                string id = EditorPrefs.GetString(P_UserId, string.Empty);
                if (string.IsNullOrEmpty(id) || id.Length != 32)
                {
                    id = Guid.NewGuid().ToString("N");
                    EditorPrefs.SetString(P_UserId, id);
                }
                return id;
            }
        }

        public static string UserName
        {
            get => EditorPrefs.GetString(P_UserName, Environment.UserName);
            set => EditorPrefs.SetString(P_UserName, value);
        }

        public static string UserColorHtml
        {
            get => EditorPrefs.GetString(P_Color, "#33AAFF");
            set => EditorPrefs.SetString(P_Color, value);
        }

        public static Color UserColor
        {
            get
            {
                if (ColorUtility.TryParseHtmlString(UserColorHtml, out Color c))
                    return c;

                return new Color(0.2f, 0.67f, 1f);
            }
            set => UserColorHtml = "#" + ColorUtility.ToHtmlStringRGB(value);
        }

        public static int Port
        {
            get => EditorPrefs.GetInt(P_Port, 7777);
            set => EditorPrefs.SetInt(P_Port, value);
        }

        public static string HostIp
        {
            get => EditorPrefs.GetString(P_HostIp, "127.0.0.1");
            set => EditorPrefs.SetString(P_HostIp, value);
        }

        public static event Action<CollabEnvelope, IPEndPoint> OnEnvelope;
        public static event Action<string> OnLog;
        public static event Action OnUsersChanged;

        public static IReadOnlyDictionary<string, CollabClientInfo> Clients => _clients;
        public static List<UserInfo> RemoteUsers => new(_remoteUsers);
        public static IReadOnlyDictionary<string, CollabClientInfo> PendingJoins => _pendingJoins;
        public static string LastDenyReason { get; private set; } = string.Empty;

        public static bool RequireApproval
        {
            get => EditorPrefs.GetBool(P_RequireApproval, true);
            set
            {
                EditorPrefs.SetBool(P_RequireApproval, value);
                if (!value && Mode == CollabMode.Host)
                {
                    List<string> waiting = new(_pendingJoins.Keys);
                    foreach (string id in waiting)
                        ApproveJoin(id);
                }

                try
                {
                    OnUsersChanged?.Invoke();
                } catch { }
            }
        }

        public static void ClearDenyReason()
        {
            LastDenyReason = string.Empty;
        }

        private static CollabUdpTransport _transport;
        private static IPEndPoint _hostEndpoint;
        private static readonly Dictionary<string, CollabClientInfo> _clients = new();
        private static readonly Dictionary<string, CollabClientInfo> _pendingJoins = new();
        private static readonly HashSet<string> _approvedIds = new();
        private static readonly HashSet<string> _deniedIds = new();
        private static readonly List<UserInfo> _remoteUsers = new();
        private static readonly Dictionary<string, PendingMsg> _pending = new();
        private static readonly HashSet<string> _seenReliable = new();
        private static long _hostSeq;
        private static double _nextHeartbeat;
        private static double _nextHello;
        private static long _lastWelcomeTicks;
        private static readonly Dictionary<string, long> _lastSnapshotTicks = new();
        private const long SnapshotResendCooldownTicks = 5L * 10000000L;
        private const double PendingJoinExpirySeconds = 120.0;

        private class PendingMsg
        {
            public string json;
            public IPEndPoint dest;
            public double lastSend;
            public int retries;
        }

        static CollabNet()
        {
            EditorApplication.update += Pump;
            EditorApplication.quitting += Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }

        public static void Log(string s)
        {
            Debug.Log($"[Collab] {s}");
            try
            {
                OnLog?.Invoke(s);
            } catch { }
        }

        public static bool StartHost(int port)
        {
            Stop();
            _transport = new CollabUdpTransport();
            if (!_transport.Bind(port))
                return false;

            Mode = CollabMode.Host;
            Welcomed = true;
            Port = port;
            _hostSeq = 0;
            _clients.Clear();
            _pendingJoins.Clear();
            _approvedIds.Clear();
            _deniedIds.Clear();
            _pending.Clear();
            _seenReliable.Clear();
            _remoteUsers.Clear();
            CollabTracker.NotifySessionStarted();
            Log($"Hosting on UDP {port} as {UserName}" + (RequireApproval ? " (approval required)" : " (auto-allow)"));
            try
            {
                OnUsersChanged?.Invoke();
            } catch { }
            return true;
        }

        public static bool StartClient(string hostIp, int port)
        {
            if (!TryParseHostInput(hostIp, port, out string host, out int resolvedPort, out string parseError))
            {
                Log($"Bad host: {parseError}");
                return false;
            }

            if (!TryResolveEndpoint(host, resolvedPort, out IPEndPoint endpoint, out string resolveError))
            {
                Log($"Could not resolve '{host}': {resolveError}");
                return false;
            }

            Stop();
            _transport = new CollabUdpTransport();
            if (!_transport.Bind(0, endpoint.AddressFamily))
                return false;

            _hostEndpoint = endpoint;
            Mode = CollabMode.Client;
            Welcomed = false;
            HostIp = hostIp?.Trim() ?? string.Empty;
            Port = resolvedPort;
            LastDenyReason = string.Empty;
            _pending.Clear();
            _seenReliable.Clear();
            _remoteUsers.Clear();
            _nextHello = 0;
            CollabTracker.NotifySessionStarted();
            string shown = host.Contains(':') && !host.StartsWith("[") ? $"[{host}]" : host;
            if (endpoint.Address.ToString() != host)
            {
                Log($"Joining {shown}:{resolvedPort} ({endpoint.Address}) as {UserName} (UDP)");
            }
            else
                Log($"Joining {host}:{resolvedPort} as {UserName} (UDP)");

            return true;
        }

        public static bool TryParseHostInput(string input, int defaultPort, out string host, out int port, out string error)
        {
            host = string.Empty;
            port = defaultPort;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "empty host";
                return false;
            }

            string s = input.Trim();

            if (s.StartsWith("["))
            {
                int close = s.IndexOf(']');
                if (close < 0)
                {
                    error = "bad IPv6 bracket";
                    return false;
                }

                host = s.Substring(1, close - 1).Trim();
                string rest = s.Substring(close + 1).Trim();
                if (rest.Length > 0)
                {
                    if (!rest.StartsWith(":"))
                    {
                        error = $"bad host '{s}'";
                        return false;
                    }

                    if (!TryParsePort(rest.Substring(1), out port, out error))
                        return false;
                }
            }
            else
            {
                int colonCount = 0;
                foreach (char c in s)
                    if (c == ':') colonCount++;

                if (colonCount == 1)
                {
                    int colon = s.LastIndexOf(':');
                    string maybeHost = s.Substring(0, colon).Trim();
                    string maybePort = s.Substring(colon + 1).Trim();
                    if (string.IsNullOrEmpty(maybeHost))
                    {
                        error = "empty host";
                        return false;
                    }

                    if (!TryParsePort(maybePort, out port, out error))
                        return false;

                    host = maybeHost;
                }
                else
                {
                    host = s;
                    port = defaultPort;
                }
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                error = "empty host";
                return false;
            }

            if (port < 1 || port > 65535)
            {
                error = $"port {port} out of range (1-65535)";
                return false;
            }

            return true;
        }

        private static bool TryParsePort(string s, out int port, out string error)
        {
            port = 0;
            error = string.Empty;
            if (!int.TryParse(s?.Trim(), out port) || port < 1 || port > 65535)
            {
                error = $"bad port '{s}' (expected 1-65535)";
                return false;
            }

            return true;
        }

        public static bool TryResolveEndpoint(string host, int port, out IPEndPoint endpoint, out string error)
        {
            endpoint = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(host))
            {
                error = "empty host";
                return false;
            }

            host = host.Trim();

            if (IPAddress.TryParse(host, out IPAddress literal))
            {
                endpoint = new IPEndPoint(literal, port);
                return true;
            }

            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(host);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (addresses == null || addresses.Length == 0)
            {
                error = "DNS returned no addresses";
                return false;
            }

            foreach (IPAddress a in addresses)
            {
                if (a.AddressFamily == AddressFamily.InterNetwork)
                {
                    endpoint = new IPEndPoint(a, port);
                    return true;
                }
            }

            endpoint = new IPEndPoint(addresses[0], port);
            return true;
        }

        public static void Stop()
        {
            if (Mode != CollabMode.Off && _transport != null)
            {
                try
                {
                    CollabEnvelope env = NewEnvelope(CollabTypes.Bye, string.Empty, false);
                    string json = CollabJson.Serialize(env);
                    if (Mode == CollabMode.Client && _hostEndpoint != null)
                    {
                        _transport.Send(json, _hostEndpoint);
                    }
                    else if (Mode == CollabMode.Host)
                    {
                        foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                        {
                            _transport.Send(json, kv.Value.endpoint);
                        }
                    }
                }
                catch { }
            }
            try
            {
                _transport?.Close();
            } catch { }

            _transport = null;
            Mode = CollabMode.Off;
            Welcomed = false;
            _clients.Clear();
            _pendingJoins.Clear();
            _remoteUsers.Clear();
            _pending.Clear();
            _lastSnapshotTicks.Clear();
            _hostEndpoint = null;
            try
            {
                OnUsersChanged?.Invoke();
            } catch { }
        }

        public static void BroadcastReliable(string type, string dataJson)
        {
            if (!Connected)
                return;

            if (Mode == CollabMode.Host)
            {
                CollabEnvelope env = NewEnvelope(type, dataJson, true);
                env.seq = ++_hostSeq;
                string json = CollabJson.Serialize(env);
                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    SendRawReliable(json, env.msgId, kv.Value.endpoint);
                }
            }
            else
                SendToHostReliable(type, dataJson);
        }

        public static void BroadcastUnreliable(string type, string dataJson)
        {
            if (!Connected)
                return;

            if (Mode == CollabMode.Host)
            {
                CollabEnvelope env = NewEnvelope(type, dataJson, false);
                env.seq = ++_hostSeq;
                string json = CollabJson.Serialize(env);
                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    _transport.Send(json, kv.Value.endpoint);
                }
            }
            else
                SendToHostUnreliable(type, dataJson);
        }

        public static void SendToHostReliable(string type, string dataJson)
        {
            if (Mode != CollabMode.Client || _hostEndpoint == null)
                return;

            CollabEnvelope env = NewEnvelope(type, dataJson, true);
            SendRawReliable(CollabJson.Serialize(env), env.msgId, _hostEndpoint);
        }

        public static void SendToHostUnreliable(string type, string dataJson)
        {
            if (Mode != CollabMode.Client || _hostEndpoint == null)
                return;

            CollabEnvelope env = NewEnvelope(type, dataJson, false);
            _transport.Send(CollabJson.Serialize(env), _hostEndpoint);
        }

        private static void SendRawReliable(string json, string msgId, IPEndPoint dest)
        {
            if (dest == null || _transport == null)
                return;

            _transport.Send(json, dest);
            lock (_pending)
            {
                _pending[msgId] = new PendingMsg
                {
                    json = json,
                    dest = dest,
                    lastSend = EditorApplication.timeSinceStartup,
                    retries = 0
                };
            }
        }

        private static CollabEnvelope NewEnvelope(string type, string dataJson, bool reliable)
        {
            return new CollabEnvelope
            {
                v = 1,
                type = type,
                msgId = reliable ? CollabJson.NewMsgId() : string.Empty,
                sender = UserId,
                senderName = UserName,
                senderColor = UserColorHtml,
                seq = 0,
                ts = CollabJson.NowTicks(),
                data = dataJson ?? string.Empty
            };
        }

        public static void SendSnapshotTo(IPEndPoint dest, string snapshotId, List<SnapshotChunkData> chunks)
        {
            if (Mode != CollabMode.Host || dest == null)
                return;

            WelcomeData welcome = new()
            {
                snapshotId = snapshotId,
                totalChunks = chunks.Count,
                builderCount = UnityEngine.Object.FindObjectsByType<Builder>(FindObjectsSortMode.None).Length,
                hostSeq = _hostSeq
            };

            CollabEnvelope wenv = NewEnvelope(CollabTypes.Welcome, CollabJson.Serialize(welcome), true);
            SendRawReliable(CollabJson.Serialize(wenv), wenv.msgId, dest);
            foreach (SnapshotChunkData c in chunks)
            {
                CollabEnvelope env = NewEnvelope(CollabTypes.SnapshotChunk, CollabJson.Serialize(c), true);
                SendRawReliable(CollabJson.Serialize(env), env.msgId, dest);
            }
            
            SnapshotChunkData done = new()
            {
                snapshotId = snapshotId,
                index = chunks.Count,
                total = chunks.Count
            };

            CollabEnvelope denv = NewEnvelope(CollabTypes.SnapshotDone, CollabJson.Serialize(done), true);
            SendRawReliable(CollabJson.Serialize(denv), denv.msgId, dest);
            Log($"Snapshot {snapshotId.Substring(0, 6)} -> {dest} ({chunks.Count} chunks)");
        }

        public static void PushSnapshotToAll()
        {
            if (Mode != CollabMode.Host || _transport == null)
                return;

            if (_clients.Count == 0)
            {
                Log("No clients to push to.");
                return;
            }
            string snapshotId = Guid.NewGuid().ToString("N");
            List<SnapshotChunkData> chunks = CollabSnapshot.BuildChunks(snapshotId, 24);
            foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
            {
                if (kv.Value?.endpoint != null)
                    SendSnapshotTo(kv.Value.endpoint, snapshotId, chunks);
            }
        }

        public static bool ApproveJoin(string userId)
        {
            if (Mode != CollabMode.Host)
                return false;

            if (string.IsNullOrEmpty(userId))
                return false;

            _deniedIds.Remove(userId);
            _approvedIds.Add(userId);

            CollabClientInfo info = null;
            if (_pendingJoins.TryGetValue(userId, out CollabClientInfo pending))
                info = pending;

            if (info == null && _clients.TryGetValue(userId, out CollabClientInfo existing))
                info = existing;

            if (info == null)
                return false;

            _pendingJoins.Remove(userId);

            if (!_clients.TryGetValue(userId, out CollabClientInfo ci))
            {
                ci = new CollabClientInfo { userId = userId };
                _clients[userId] = ci;
            }

            ci.name = info.name;
            ci.color = info.color;
            ci.endpoint = info.endpoint;
            ci.lastSeenTicks = DateTime.UtcNow.Ticks;

            Log($"{ci.name} approved ({userId.Substring(0, Math.Min(6, userId.Length))})");
            try
            {
                OnUsersChanged?.Invoke();
            } catch { }

            try
            {
                string snapshotId = Guid.NewGuid().ToString("N");
                List<SnapshotChunkData> chunks = CollabSnapshot.BuildChunks(snapshotId, 24);
                IPEndPoint dest = ci.endpoint;
                _lastSnapshotTicks[userId] = DateTime.UtcNow.Ticks;
                EditorApplication.delayCall += () =>
                {
                    if (Mode == CollabMode.Host)
                        SendSnapshotTo(dest, snapshotId, chunks);
                };
            }
            catch (Exception ex)
            {
                Log($"Snapshot build failed: {ex.Message}");
            }

            return true;
        }

        public static bool DenyJoin(string userId, string reason = "denied by host")
        {
            if (Mode != CollabMode.Host)
                return false;

            if (string.IsNullOrEmpty(userId))
                return false;

            IPEndPoint dest = null;
            string name = userId;
            if (_pendingJoins.TryGetValue(userId, out CollabClientInfo pending))
            {
                dest = pending.endpoint;
                name = pending.name;
                _pendingJoins.Remove(userId);
            }

            if (_clients.TryGetValue(userId, out CollabClientInfo client))
            {
                dest ??= client.endpoint;
                name = client.name;
                _clients.Remove(userId);
            }

            _approvedIds.Remove(userId);
            _deniedIds.Add(userId);
            _lastSnapshotTicks.Remove(userId);

            Log($"{name} denied ({reason})");
            try
            {
                OnUsersChanged?.Invoke();
            } catch { }

            if (dest != null && _transport != null)
                SendDeny(dest, reason);

            return true;
        }

        public static bool KickClient(string userId)
        {
            return DenyJoin(userId, "kicked by host");
        }

        private static void SendDeny(IPEndPoint dest, string reason)
        {
            if (dest == null || _transport == null)
                return;

            try
            {
                CollabEnvelope env = NewEnvelope(CollabTypes.JoinDenied, CollabJson.Serialize(new ErrorData { reason = reason }), true);
                SendRawReliable(CollabJson.Serialize(env), env.msgId, dest);
            }
            catch { }
        }

        private static void Pump()
        {
            if (Mode == CollabMode.Off || _transport == null)
                return;

            _transport.Pump(OnDatagram);
            RetryPending();
            Heartbeat();
            if (Mode == CollabMode.Client && !Welcomed)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now >= _nextHello)
                {
                    _nextHello = now + 1.0;
                    HelloData hello = new()
                    {
                        userId = UserId,
                        userName = UserName,
                        color = UserColorHtml,
                        unityVersion = Application.unityVersion
                    };

                    CollabEnvelope env = NewEnvelope(CollabTypes.Hello, CollabJson.Serialize(hello), true);
                    SendRawReliable(CollabJson.Serialize(env), env.msgId, _hostEndpoint);
                }
            }
        }

        private static void RetryPending()
        {
            if (_pending.Count == 0)
                return;

            double now = EditorApplication.timeSinceStartup;
            List<string> drop = null;
            lock (_pending)
            {
                foreach (KeyValuePair<string, PendingMsg> kv in _pending)
                {
                    if (now - kv.Value.lastSend < 0.5)
                        continue;

                    if (kv.Value.retries >= 20)
                    {
                        (drop ??= new List<string>()).Add(kv.Key);
                        continue;
                    }

                    kv.Value.retries++;
                    kv.Value.lastSend = now;
                    try
                    {
                        _transport.Send(kv.Value.json, kv.Value.dest);
                    } catch { }
                }
                if (drop != null)
                {
                    foreach (string k in drop)
                    {
                        _pending.Remove(k);
                    }
                }
            }
            if (drop != null && drop.Count > 0)
                Log($"Dropped {drop.Count} unacked packet(s)");
        }

        private static void Heartbeat()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextHeartbeat)
                return;

            _nextHeartbeat = now + 2.0;
            if (Mode == CollabMode.Host)
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                List<string> stale = new();
                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    if (new TimeSpan(nowTicks - kv.Value.lastSeenTicks).TotalSeconds > 12)
                    {
                        stale.Add(kv.Key);
                    }
                }

                foreach (string s in stale)
                {
                    _clients.Remove(s);
                }

                List<string> expiredJoins = new();
                foreach (KeyValuePair<string, CollabClientInfo> kv in _pendingJoins)
                {
                    if (new TimeSpan(nowTicks - kv.Value.lastSeenTicks).TotalSeconds > PendingJoinExpirySeconds)
                        expiredJoins.Add(kv.Key);
                }

                foreach (string s in expiredJoins)
                {
                    _pendingJoins.Remove(s);
                }

                if (stale.Count > 0 || expiredJoins.Count > 0)
                {
                    try
                    {
                        OnUsersChanged?.Invoke();
                    } catch { }
                }

                List<UserInfo> users = new()
                {
                    new() { id = UserId, name = UserName + " (host)", color = UserColorHtml }
                };

                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    users.Add(new UserInfo { id = kv.Value.userId, name = kv.Value.name, color = kv.Value.color });
                }

                CollabEnvelope env = NewEnvelope(CollabTypes.Presence, CollabJson.Serialize(new PresenceData { users = users }), false);
                string json = CollabJson.Serialize(env);
                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    _transport.Send(json, kv.Value.endpoint);
                }

                _remoteUsers.Clear();
                foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                {
                    _remoteUsers.Add(new UserInfo { id = kv.Value.userId, name = kv.Value.name, color = kv.Value.color });
                }

                try
                {
                    OnUsersChanged?.Invoke();
                } catch { }
            }
            else if (Mode == CollabMode.Client && _hostEndpoint != null)
            {
                CollabEnvelope env = NewEnvelope(CollabTypes.Ping, string.Empty, false);
                _transport.Send(CollabJson.Serialize(env), _hostEndpoint);
            }
        }

        private static void OnDatagram(string json, IPEndPoint from)
        {
            CollabEnvelope env;
            try
            {
                env = CollabJson.Deserialize<CollabEnvelope>(json);
            }
            catch
            {
                return;
            }

            if (env == null || string.IsNullOrEmpty(env.type))
                return;

            if (env.type == CollabTypes.Ack)
            {
                lock (_pending)
                {
                    _pending.Remove(env.ackFor);
                }
                return;
            }

            bool reliable = CollabTypes.IsReliable(env.type);
            if (reliable && !string.IsNullOrEmpty(env.msgId))
            {
                CollabEnvelope ack = new() { type = CollabTypes.Ack, sender = UserId, ackFor = env.msgId, ts = CollabJson.NowTicks() };
                try
                {
                    _transport.Send(CollabJson.Serialize(ack), from);
                } catch { }
                lock (_seenReliable)
                {
                    if (!_seenReliable.Add(env.msgId))
                        return;

                    if (_seenReliable.Count > 5000)
                        _seenReliable.Clear();
                }
            }

            if (Mode == CollabMode.Host && env.type == CollabTypes.Hello)
            {
                HandleHello(env, from);
                return;
            }

            if (Mode == CollabMode.Host && RequireApproval && !string.IsNullOrEmpty(env.sender) && !_clients.ContainsKey(env.sender))
            {
                if (_approvedIds.Contains(env.sender) && !_deniedIds.Contains(env.sender))
                {
                    CollabClientInfo rejoin = new()
                    {
                        userId = env.sender,
                        name = string.IsNullOrEmpty(env.senderName) ? "guest" : env.senderName,
                        color = string.IsNullOrEmpty(env.senderColor) ? "#33AAFF" : env.senderColor,
                        endpoint = from,
                        lastSeenTicks = DateTime.UtcNow.Ticks
                    };
                    _clients[env.sender] = rejoin;
                    Log($"{rejoin.name} rejoined");
                    try
                    {
                        OnUsersChanged?.Invoke();
                    } catch { }
                }
                else
                    return;
            }

            if (Mode == CollabMode.Host && !string.IsNullOrEmpty(env.sender) && _clients.TryGetValue(env.sender, out CollabClientInfo ci))
            {
                ci.endpoint = from;
                ci.lastSeenTicks = DateTime.UtcNow.Ticks;
                if (!string.IsNullOrEmpty(env.senderName))
                    ci.name = env.senderName;

                if (!string.IsNullOrEmpty(env.senderColor))
                    ci.color = env.senderColor;
            }

            switch (env.type)
            {
                case CollabTypes.Hello:
                    break;

                case CollabTypes.Welcome or CollabTypes.SnapshotChunk or CollabTypes.SnapshotDone:
                    if (Mode == CollabMode.Client)
                    {
                        _lastWelcomeTicks = DateTime.UtcNow.Ticks;
                        if (env.type == CollabTypes.Welcome)
                            Welcomed = true;

                        if (_hostEndpoint != null && !from.Equals(_hostEndpoint))
                            break;

                        Dispatch(env);
                    }

                    break;
                case CollabTypes.Ping:
                    if (Mode == CollabMode.Host)
                    {
                        CollabEnvelope pong = new() { type = CollabTypes.Pong, sender = UserId, ts = CollabJson.NowTicks() };
                        try
                        {
                            _transport.Send(CollabJson.Serialize(pong), from);
                        } catch { }
                    }
                    break;
                case CollabTypes.Pong or CollabTypes.Presence:
                    if (Mode == CollabMode.Client)
                    {
                        if (_hostEndpoint != null && !from.Equals(_hostEndpoint))
                            break;

                        _lastWelcomeTicks = DateTime.UtcNow.Ticks;
                        Dispatch(env);
                    }
                    break;
                case CollabTypes.Bye:
                    if (Mode == CollabMode.Host && _clients.ContainsKey(env.sender))
                    {
                        _clients.Remove(env.sender);
                        Log($"{env.senderName} left");
                        try
                        {
                            OnUsersChanged?.Invoke();
                        } catch { }
                    }
                    break;
                case CollabTypes.Error:
                    Log($"Remote error: {env.data}");
                    break;
                case CollabTypes.JoinDenied:
                    if (Mode == CollabMode.Client)
                    {
                        if (_hostEndpoint != null && !from.Equals(_hostEndpoint))
                            break;

                        string reason = "denied by host";
                        try
                        {
                            ErrorData denied = CollabJson.Deserialize<ErrorData>(env.data);
                            if (denied != null && !string.IsNullOrEmpty(denied.reason))
                                reason = denied.reason;
                        }
                        catch { }

                        LastDenyReason = reason;
                        Log($"Join denied: {reason}");
                        try
                        {
                            OnUsersChanged?.Invoke();
                        } catch { }

                        string savedReason = LastDenyReason;
                        Stop();
                        LastDenyReason = savedReason;
                        try
                        {
                            OnUsersChanged?.Invoke();
                        } catch { }
                    }
                    break;
                default:
                    if (Mode == CollabMode.Host)
                    {
                        env.seq = ++_hostSeq;
                        string fwd = CollabJson.Serialize(env);
                        foreach (KeyValuePair<string, CollabClientInfo> kv in _clients)
                        {
                            if (kv.Key == env.sender)
                                continue;

                            if (reliable)
                            {
                                SendRawReliable(fwd, CollabJson.NewMsgId(), kv.Value.endpoint);
                            }
                            else
                                _transport.Send(fwd, kv.Value.endpoint);
                        }
                        Dispatch(env);
                    }
                    else
                    {
                        if (_hostEndpoint != null && !from.Equals(_hostEndpoint))
                            break;

                        Dispatch(env);
                    }
                    break;
            }
        }

        private static void HandleHello(CollabEnvelope env, IPEndPoint from)
        {
            HelloData hello;
            try 
            {
                hello = CollabJson.Deserialize<HelloData>(env.data);
            }
            catch
            {
                return;
            }

            if (hello == null || string.IsNullOrEmpty(hello.userId))
                return;

            if (hello.unityVersion != Application.unityVersion)
                Log($"Version mismatch: {hello.userName} on {hello.unityVersion} (local {Application.unityVersion}). Prefabs must still match.");

            if (_deniedIds.Contains(hello.userId))
            {
                SendDeny(from, "denied by host");
                return;
            }

            if (RequireApproval && !_approvedIds.Contains(hello.userId) && !_clients.ContainsKey(hello.userId))
            {
                string wantName = string.IsNullOrEmpty(hello.userName) ? "guest" : hello.userName;
                string wantColor = string.IsNullOrEmpty(hello.color) ? "#33AAFF" : hello.color;
                bool isNew = !_pendingJoins.ContainsKey(hello.userId);
                _pendingJoins[hello.userId] = new CollabClientInfo
                {
                    userId = hello.userId,
                    name = wantName,
                    color = wantColor,
                    endpoint = from,
                    lastSeenTicks = DateTime.UtcNow.Ticks
                };

                if (isNew)
                    Log($"{wantName} requested to join ({from}) — approve in Collab window");

                try
                {
                    OnUsersChanged?.Invoke();
                } catch { }

                return;
            }

            _approvedIds.Add(hello.userId);
            _pendingJoins.Remove(hello.userId);

            if (!_clients.TryGetValue(hello.userId, out CollabClientInfo ci))
            {
                ci = new CollabClientInfo { userId = hello.userId };
                _clients[hello.userId] = ci;
                Log($"{hello.userName} joined from {from} (UDP)");
            }

            ci.name = string.IsNullOrEmpty(hello.userName) ? "guest" : hello.userName;
            ci.color = string.IsNullOrEmpty(hello.color) ? "#33AAFF" : hello.color;
            ci.endpoint = from;
            ci.lastSeenTicks = DateTime.UtcNow.Ticks;
            try
            {
                OnUsersChanged?.Invoke();
            } catch { }

            try
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                if (_lastSnapshotTicks.TryGetValue(hello.userId, out long lastTicks))
                {
                    if (nowTicks - lastTicks < SnapshotResendCooldownTicks)
                        return;
                }

                _lastSnapshotTicks[hello.userId] = nowTicks;
                string snapshotId = Guid.NewGuid().ToString("N");
                List<SnapshotChunkData> chunks = CollabSnapshot.BuildChunks(snapshotId, 24);
                IPEndPoint dest = from;
                EditorApplication.delayCall += () =>
                {
                    if (Mode == CollabMode.Host)
                        SendSnapshotTo(dest, snapshotId, chunks);
                };
            }
            catch (Exception ex)
            {
                Log($"Snapshot build failed: {ex.Message}");
            }
        }

        private static void Dispatch(CollabEnvelope env)
        {
            try
            {
                OnEnvelope?.Invoke(env, _hostEndpoint);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Collab] dispatch error: {ex.Message}");
            }
        }
    }
}
