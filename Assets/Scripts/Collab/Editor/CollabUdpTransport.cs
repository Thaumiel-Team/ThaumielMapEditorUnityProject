using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public sealed class CollabUdpTransport : IDisposable
    {
        private const int FragSlice = 800;

        [Serializable]
        private class FragOuter
        {
            public string fid;
            public int idx;
            public int total;
            public string chunk; // base64 slice
        }

        private class FragBuffer
        {
            public int total;
            public byte[][] parts;
            public int received;
            public long lastTicks = DateTime.UtcNow.Ticks;
        }

        public struct Datagram
        {
            public string json;
            public IPEndPoint from;
        }

        private UdpClient _udp;
        private Thread _recvThread;
        private volatile bool _running;
        private readonly ConcurrentQueue<Datagram> _inbox = new();
        private readonly ConcurrentQueue<string> _threadNotices = new();
        private readonly Dictionary<string, FragBuffer> _fragBuffers = new();
        private readonly object _fragLock = new();

        public int LocalPort { get; private set; }

        public bool Bind(int port)
        {
            return Bind(port, AddressFamily.InterNetwork);
        }

        public bool Bind(int port, AddressFamily family)
        {
            try
            {
                Close();
                if (port == 0)
                {
                    _udp = family == AddressFamily.InterNetworkV6 ? new UdpClient(family) : new UdpClient(0);
                }
                else
                {
                    IPAddress localAddr = family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
                    _udp = new UdpClient(new IPEndPoint(localAddr, port));
                }
                
                _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                LocalPort = ((IPEndPoint)_udp.Client.LocalEndPoint).Port;
                _running = true;
                _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "CollabUDP-Recv" };
                _recvThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CollabUDP] Bind({port}) failed: {ex.Message}");
                return false;
            }
        }

        public void Send(string json, IPEndPoint dest)
        {
            if (!_running || _udp == null || dest == null)
                return;

            try
            {
                byte[] raw = Encoding.UTF8.GetBytes(json);
                if (raw.Length <= 1000)
                {
                    _udp.Send(raw, raw.Length, dest);
                    return;
                }

                string fid = Guid.NewGuid().ToString("N");
                int total = (raw.Length + FragSlice - 1) / FragSlice;
                for (int i = 0; i < total; i++)
                {
                    int off = i * FragSlice;
                    int len = Math.Min(FragSlice, raw.Length - off);
                    byte[] slice = new byte[len];
                    Buffer.BlockCopy(raw, off, slice, 0, len);
                    FragOuter outer = new()
                    {
                        fid = fid,
                        idx = i,
                        total = total,
                        chunk = Convert.ToBase64String(slice)
                    };
                    byte[] outBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outer));
                    _udp.Send(outBytes, outBytes.Length, dest);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CollabUDP] Send failed: {ex.Message}");
            }
        }

        private void RecvLoop()
        {
            IPEndPoint remote = new(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    if (_udp == null)
                        break;

                    byte[] bytes = _udp.Receive(ref remote);
                    IPEndPoint from = new(remote.Address, remote.Port);
                    string text = Encoding.UTF8.GetString(bytes);
                    HandleIncoming(text, from);
                }
                catch (SocketException se)
                {
                    if (!_running)
                        break;

                    if (se.SocketErrorCode == SocketError.TimedOut)
                        continue;

                    Thread.Sleep(20);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                        _threadNotices.Enqueue($"[CollabUDP] Recv error: {ex.Message}");

                    Thread.Sleep(20);
                }
            }
        }

        private void HandleIncoming(string text, IPEndPoint from)
        {
            if (text.Contains("\"fid\""))
            {
                try
                {
                    FragOuter outer = JsonConvert.DeserializeObject<FragOuter>(text);
                    if (outer != null && !string.IsNullOrEmpty(outer.fid) && outer.total > 0 && !string.IsNullOrEmpty(outer.chunk))
                    {
                        byte[] slice = Convert.FromBase64String(outer.chunk);
                        string complete = null;
                        lock (_fragLock)
                        {
                            if (!_fragBuffers.TryGetValue(outer.fid, out FragBuffer buf))
                            {
                                buf = new FragBuffer { total = outer.total, parts = new byte[outer.total][] };
                                _fragBuffers[outer.fid] = buf;
                            }

                            if (outer.idx >= 0 && outer.idx < buf.total && buf.parts[outer.idx] == null)
                            {
                                buf.parts[outer.idx] = slice;
                                buf.received++;
                            }

                            buf.lastTicks = DateTime.UtcNow.Ticks;
                            if (buf.received >= buf.total)
                            {
                                int totalLen = 0;
                                foreach (byte[] p in buf.parts)
                                {
                                    totalLen += p.Length;
                                }

                                byte[] all = new byte[totalLen];
                                int off = 0;
                                foreach (byte[] p in buf.parts)
                                {
                                    Buffer.BlockCopy(p, 0, all, off, p.Length);
                                    off += p.Length;
                                }

                                complete = Encoding.UTF8.GetString(all);
                                _fragBuffers.Remove(outer.fid);
                            }

                            if (_fragBuffers.Count > 32)
                            {
                                long now = DateTime.UtcNow.Ticks;
                                List<string> stale = new();
                                foreach (KeyValuePair<string, FragBuffer> kv in _fragBuffers)
                                {
                                    if (new TimeSpan(now - kv.Value.lastTicks).TotalSeconds > 15)
                                        stale.Add(kv.Key);
                                }
                                
                                foreach (string k in stale)
                                    _fragBuffers.Remove(k);
                            }
                        }
                        if (complete != null)
                            _inbox.Enqueue(new Datagram { json = complete, from = from });

                        return;
                    }
                }
                catch { /* treat as normal packet */ }
            }
            _inbox.Enqueue(new Datagram { json = text, from = from });
        }

        public void Pump(Action<string, IPEndPoint> onDatagram)
        {
            while (_threadNotices.TryDequeue(out string notice))
            {
                try
                {    
                    Debug.LogWarning(notice);
                } catch { }
            }

            int budget = 200;
            while (budget-- > 0 && _inbox.TryDequeue(out Datagram d))
            {
                try
                {
                    onDatagram?.Invoke(d.json, d.from);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CollabUDP] handler error: {ex.Message}");
                }
            }
        }

        public void Close()
        {
            _running = false;
            try
            {
                _udp?.Close();
            } catch { }
            
            _udp = null;
            try
            {
                if (_recvThread != null && _recvThread.IsAlive)
                    _recvThread.Join(300);
            }
            catch { }
            _recvThread = null;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
