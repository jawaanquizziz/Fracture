using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Valmon.Campaign;

namespace Valmon.Editor
{
    public static class LanNetworkValidation
    {
        public static void Run()
        {
            var results = new List<string>();
            using (var host = new LanUdpTransport(0))
            using (var client = new LanUdpTransport(0, new IPEndPoint(IPAddress.Loopback, host.Port)))
            {
                host.Accept(new IPEndPoint(IPAddress.Loopback, client.Port));
                host.TestDropEvery = client.TestDropEvery = 6;
                for (int i = 0; i < 30; i++) client.Send("command:" + i, true);
                int received = 0; bool snapshot = false; var timer = Stopwatch.StartNew(); int tick = 0;
                while (timer.Elapsed.TotalSeconds < 8 && (received < 30 || !snapshot || client.PendingCount != 0))
                {
                    host.Tick(); client.Tick();
                    while (host.TryReceive(out LanUdpTransport.Message message))
                    {
                        if (message.text != "command:" + received) throw new Exception("UDP control messages duplicated or reordered: " + message.text);
                        received++;
                    }
                    if (++tick % 10 == 0) host.Send(new string('s', 11000) + "complete", false);
                    while (client.TryReceive(out LanUdpTransport.Message message))
                        if (message.text.Length == 11008 && message.text.EndsWith("complete")) snapshot = true;
                    Thread.Sleep(2);
                }
                if (received != 30 || client.PendingCount != 0) throw new Exception("UDP reliability test did not acknowledge all ordered commands.");
                if (!snapshot) throw new Exception("UDP fragmented snapshot was not reassembled.");
                if (client.Retries == 0) throw new Exception("Loss simulation did not exercise retransmission.");
                results.Add("PASS 30 UDP commands arrived once, in order, and were acknowledged under simulated 1-in-6 packet loss.");
                results.Add("PASS 11 KB snapshots reassembled from datagrams below 1,200 bytes under packet loss.");
                results.Add("PASS Retransmission exercised; pending reliable queue drained to zero.");
            }
            Directory.CreateDirectory("Tools/Reports"); File.WriteAllLines("Tools/Reports/udp-transport-validation.txt", results);
            UnityEngine.Debug.Log("[VALOMON UDP] Transport validation passed.");
        }
    }
}
