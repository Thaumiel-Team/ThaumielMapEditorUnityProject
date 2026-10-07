using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace Assets.Scripts.Networking.Blocky
{
    internal class WsConnection
    {
        public bool IsOpen { get; private set; }

        private readonly TcpClient _tcp;
        private readonly NetworkStream _stream;
        private readonly Action<string> _onMessage;
        private readonly Action _onClose;

        private readonly ConcurrentQueue<byte[]> _sendQueue = new();

        private Thread _readThread;
        private Thread _writeThread;

        public WsConnection(TcpClient tcp, Action<string> onMessage, Action onClose)
        {
            _tcp = tcp;
            _stream = tcp.GetStream();
            _onMessage = onMessage;
            _onClose = onClose;
        }

        public void Start()
        {
            _readThread = new Thread(ReadLoop) { IsBackground = true, Name = "BlocklyBridge-Read" };
            _readThread.Start();

            _writeThread = new Thread(WriteLoop) { IsBackground = true, Name = "BlocklyBridge-Write" };
            _writeThread.Start();
        }

        public void Close()
        {
            IsOpen = false;
            try
            {
                _tcp?.Close();
            }
            catch { }
        }

        public void SendText(string text)
        {
            if (!IsOpen)
                return;

            byte[] frame = BuildFrame(0x1, Encoding.UTF8.GetBytes(text));
            _sendQueue.Enqueue(frame);
        }

        private void WriteLoop()
        {
            while (true)
            {
                if (!IsOpen)
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (_sendQueue.TryDequeue(out byte[] frame))
                {
                    try
                    {
                        lock (_stream)
                        {
                            _stream.Write(frame, 0, frame.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BlocklyBridge] Send error: {ex.Message}");
                        Close();
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void ReadLoop()
        {
            try
            {
                if (!Handshake())
                    return;

                IsOpen = true;
                while (IsOpen)
                {
                    string msg = ReadFrame();
                    if (msg == null)
                        break;

                    if (msg != "")
                        _onMessage(msg);
                }
            }
            catch (Exception ex)
            {
                if (IsOpen)
                    Debug.LogWarning($"[BlocklyBridge] Read error: {ex.Message}");
            }
            finally
            {
                IsOpen = false;
                Close();
                _onClose?.Invoke();
            }
        }

        private bool Handshake()
        {
            StringBuilder sb = new();
            byte[] buf = new byte[4096];

            int total = 0;
            while (true)
            {
                int n = _stream.Read(buf, total, buf.Length - total);
                if (n == 0)
                    return false;

                total += n;
                string partial = Encoding.UTF8.GetString(buf, 0, total);
                if (partial.Contains("\r\n\r\n"))
                {
                    sb.Append(partial);
                    break;
                }

                if (total >= buf.Length)
                    return false;
            }

            string request = sb.ToString();
            Match keyMatch = Regex.Match(request, @"Sec-WebSocket-Key:\s*(.+)\r\n");
            if (!keyMatch.Success)
                return false;

            string key = keyMatch.Groups[1].Value.Trim();
            string accept = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

            string response =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Accept: {accept}\r\n\r\n";

            byte[] resp = Encoding.UTF8.GetBytes(response);
            _stream.Write(resp, 0, resp.Length);
            return true;
        }

        private string ReadFrame()
        {
            int b0 = _stream.ReadByte();
            if (b0 < 0)
                return null;

            // Byte 1: MASK + payload length
            int b1 = _stream.ReadByte();
            if (b1 < 0)
                return null;

            bool masked = (b1 & 0x80) != 0;
            int opcode = b0 & 0x0F;
            long payLen = b1 & 0x7F;

            if (payLen == 126)
            {
                payLen = (ReadByte() << 8) | ReadByte();
            }
            else if (payLen == 127)
            {
#pragma warning disable CS0675
                payLen = 0;
                for (int i = 0; i < 8; i++)
                {
                    payLen = (payLen << 8) | ReadByte();
                }
            }

            byte[] mask = masked ? new byte[] { (byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte() } : null;
            byte[] payload = ReadExact((int)payLen);

            if (masked && mask != null)
            {
                for (int i = 0; i < payload.Length; i++)
                {
                    payload[i] ^= mask[i % 4];
                }
            }

            switch (opcode)
            {
                case 0x1:
                    return Encoding.UTF8.GetString(payload); // text

                case 0x2:
                    return ""; // binary – ignored

                case 0x8:
                    return null; // close

                case 0x9:
                    SendPong(payload); return ""; // ping

                default:
                    return "";
            }
        }

        private void SendPong(byte[] payload)
        {
            byte[] frame = BuildFrame(0xA, payload); // opcode 0xA = pong
            try
            {
                lock (_stream)
                {
                    _stream.Write(frame, 0, frame.Length);
                }
            }
            catch { }
        }

        private static byte[] BuildFrame(byte opcode, byte[] payload)
        {
            int len = payload.Length;
            byte[] header;

            if (len <= 125)
            {
                header = new byte[2];
                header[1] = (byte)len;
            }
            else if (len <= 65535)
            {
                header = new byte[4];
                header[1] = 126;
                header[2] = (byte)(len >> 8);
                header[3] = (byte)(len & 0xFF);
            }
            else
            {
                header = new byte[10];
                header[1] = 127;
                header[2] = 0;
                header[3] = 0;
                header[4] = 0;
                header[5] = 0;
                header[6] = (byte)((len >> 24) & 0xFF);
                header[7] = (byte)((len >> 16) & 0xFF);
                header[8] = (byte)((len >> 8) & 0xFF);
                header[9] = (byte)(len & 0xFF);
            }

            header[0] = (byte)(0x80 | opcode); // FIN=1

            byte[] frame = new byte[header.Length + len];
            Array.Copy(header, 0, frame, 0, header.Length);
            Array.Copy(payload, 0, frame, header.Length, len);
            return frame;
        }

        private int ReadByte()
        {
            int b = _stream.ReadByte();
            if (b < 0)
                throw new EndOfStreamException("WebSocket stream closed.");

            return b;
        }

        private byte[] ReadExact(int count)
        {
            byte[] buf = new byte[count];
            int got = 0;
            while (got < count)
            {
                int n = _stream.Read(buf, got, count - got);
                if (n == 0)
                    throw new EndOfStreamException("WebSocket stream closed.");

                got += n;
            }
            return buf;
        }
    }
}