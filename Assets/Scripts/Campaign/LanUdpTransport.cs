using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Valmon.Campaign
{
    // UDP only. Reliable ordered control and fragmented, sequenced snapshots use separate lanes.
    // Datagrams stay below 1,200 bytes to avoid IP fragmentation on ordinary LANs.
    public sealed class LanUdpTransport : IDisposable
    {
        private const uint Magic = 0x56414C4D;
        private const int Header = 18, Payload = 1100, MaxParts = 60;
        private readonly UdpClient socket;
        private readonly Thread thread;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly ConcurrentQueue<Datagram> incoming = new ConcurrentQueue<Datagram>();
        private readonly Queue<Message> messages = new Queue<Message>();
        private readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
        private readonly Dictionary<long, Assembly> assemblies = new Dictionary<long, Assembly>();
        private readonly SortedDictionary<int, Message> ordered = new SortedDictionary<int, Message>();
        private volatile bool running = true;
        private int reliableId, snapshotId, nextReliable = 1, latestSnapshot;
        private int sentCount;
        private readonly Random testRandom = new Random(42);
        public IPEndPoint Peer { get; private set; }
        public int Port => ((IPEndPoint)socket.Client.LocalEndPoint).Port;
        public string Error { get; private set; }
        public int PendingCount => pending.Count;
        public int ReceivedMessages { get; private set; }
        public int Retries { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public int TestDropEvery;
#endif
        public struct Message { public string text; public IPEndPoint from; public bool reliable; }
        private struct Datagram { public byte[] data; public IPEndPoint from; }
        private sealed class Pending { public byte[][] packets; public double first, last; }
        private sealed class Assembly { public byte[][] parts; public int count; public double created; public IPEndPoint from; }

        public LanUdpTransport(int port, IPEndPoint peer = null)
        {
            socket = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                socket.Client.ReceiveBufferSize = 262144;
                socket.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                Peer = peer;
                thread = new Thread(Receive) { IsBackground = true, Name = "VALOMON UDP" };
                thread.Start();
            }
            catch { socket.Close(); throw; }
        }
        public void Accept(IPEndPoint peer) { if (Peer == null) Peer = peer; }
        private static bool Same(IPEndPoint a, IPEndPoint b) => a != null && b != null && a.Port == b.Port && a.Address.Equals(b.Address);
        private void Receive()
        {
            while (running)
            {
                try
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = socket.Receive(ref from);
                    if (data.Length >= Header && data.Length <= Header + Payload && incoming.Count < 1024)
                        incoming.Enqueue(new Datagram { data = data, from = from });
                }
                catch (SocketException) { if (!running) break; }
                catch (ObjectDisposedException) { break; }
            }
        }
        public void Send(string text, bool reliable, IPEndPoint destination = null)
        {
            IPEndPoint target = destination ?? Peer;
            if (target == null || !running) return;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            int count = Math.Max(1, (bytes.Length + Payload - 1) / Payload);
            if (count > MaxParts || (reliable && pending.Count >= 64)) { Error = "Network send queue exceeded its limit."; return; }
            int id = reliable ? ++reliableId : ++snapshotId;
            byte[][] packets = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                int length = Math.Min(Payload, bytes.Length - i * Payload);
                using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Magic); writer.Write((byte)1); writer.Write((byte)(reliable ? 1 : 0));
                    writer.Write(id); writer.Write((ushort)i); writer.Write((ushort)count); writer.Write(length);
                    writer.Write(bytes, i * Payload, length); packets[i] = stream.ToArray();
                }
                SendBytes(packets[i], target);
            }
            if (reliable) pending[id] = new Pending { packets = packets, first = clock.Elapsed.TotalSeconds, last = clock.Elapsed.TotalSeconds };
        }
        private void SendBytes(byte[] data, IPEndPoint target)
        {
            if (!running) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TestDropEvery > 0 && testRandom.Next(TestDropEvery) == 0) return;
#endif
            try { socket.Send(data, data.Length, target); }
            catch (SocketException e) { Error = "UDP send failed: " + e.SocketErrorCode; }
            catch (ObjectDisposedException) { }
        }
        private void Ack(int id, IPEndPoint target)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write((byte)1); writer.Write((byte)2); writer.Write(id);
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write(0);
                SendBytes(stream.ToArray(), target);
            }
        }
        public void Tick()
        {
            double now = clock.Elapsed.TotalSeconds;
            for (int budget = 0; budget < 256 && incoming.TryDequeue(out Datagram raw); budget++)
            {
                if (Peer != null && !Same(Peer, raw.from)) continue;
                try
                {
                    using (var reader = new BinaryReader(new MemoryStream(raw.data)))
                    {
                        if (reader.ReadUInt32() != Magic || reader.ReadByte() != 1) continue;
                        byte flags = reader.ReadByte(); int id = reader.ReadInt32();
                        int part = reader.ReadUInt16(), count = reader.ReadUInt16(), length = reader.ReadInt32();
                        if (id <= 0 || count < 1 || count > MaxParts || part >= count || length < 0 || length > Payload || raw.data.Length != Header + length) continue;
                        if (flags == 2) { if (Same(Peer, raw.from)) pending.Remove(id); continue; }
                        if (flags > 1) continue;
                        bool reliable = flags == 1;
                        if (Peer == null && (reliable || count != 1)) continue; // Only a small untrusted HELLO may reach the host lobby.
                        if (Peer == null)
                        {
                            messages.Enqueue(new Message { text = Encoding.UTF8.GetString(reader.ReadBytes(length)), from = raw.from });
                            continue;
                        }
                        if (reliable && id < nextReliable) { Ack(id, raw.from); continue; }
                        if (reliable && id > nextReliable + 128) continue;
                        if (!reliable && id <= latestSnapshot) continue;
                        long key = ((long)id << 1) | flags;
                        if (!assemblies.TryGetValue(key, out Assembly assembly))
                        {
                            if (assemblies.Count >= 64) continue;
                            assembly = new Assembly { parts = new byte[count][], created = now, from = raw.from };
                            assemblies[key] = assembly;
                        }
                        if (assembly.parts.Length != count || !Same(assembly.from, raw.from)) continue;
                        if (assembly.parts[part] == null) { assembly.parts[part] = reader.ReadBytes(length); assembly.count++; }
                        if (assembly.count != count) continue;
                        assemblies.Remove(key);
                        using (var full = new MemoryStream())
                        {
                            foreach (byte[] piece in assembly.parts) full.Write(piece, 0, piece.Length);
                            var message = new Message { text = Encoding.UTF8.GetString(full.ToArray()), from = raw.from, reliable = reliable };
                            if (reliable)
                            {
                                Ack(id, raw.from); ordered[id] = message;
                                while (ordered.TryGetValue(nextReliable, out Message ready)) { ordered.Remove(nextReliable++); messages.Enqueue(ready); ReceivedMessages++; }
                            }
                            else { latestSnapshot = id; messages.Enqueue(message); ReceivedMessages++; }
                        }
                    }
                }
                catch (IOException) { } catch (ArgumentException) { }
            }
            var expired = new List<long>();
            foreach (var entry in assemblies) if (now - entry.Value.created > 2) expired.Add(entry.Key);
            foreach (long id in expired) assemblies.Remove(id);
            foreach (Pending message in pending.Values)
            {
                if (now - message.first > 12) { Error = "The other laptop stopped acknowledging messages."; break; }
                if (now - message.last < .18) continue;
                foreach (byte[] packet in message.packets) SendBytes(packet, Peer);
                message.last = now; Retries++;
            }
        }
        public bool TryReceive(out Message message)
        {
            if (messages.Count > 0) { message = messages.Dequeue(); return true; }
            message = default; return false;
        }
        public void Dispose()
        {
            running = false; socket.Close();
            if (thread != null && thread.IsAlive) thread.Join(250);
        }
    }
}
