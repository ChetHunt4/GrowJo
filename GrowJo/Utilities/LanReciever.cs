using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GrowJo.Utilities
{
    public class LanReciever
    {
        private readonly int udpPort = 5051;
        private readonly int tcpPort = 5050;
        private UdpClient? udp;
        private TcpListener? tcp;
        private string _contentFolder { get; set; }

        public LanReciever(string content) 
        {
            _contentFolder = content;
        }

        public event Action<string>? FileReceived;

        public void Start()
        {
            StartBroadcast();
            StartTcpServer();
        }

        private void StartBroadcast()
        {
            udp = new UdpClient();
            udp.EnableBroadcast = true;
            Task.Run(async () =>
            {
                while (true)
                {
                    var ip = GetLocalIPAddress();
                    var message = $"GROJORECEIVER:{ip}:{tcpPort}";
                    var data = Encoding.UTF8.GetBytes(message);
                    udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));
                    await Task.Delay(2000); // every 2 seconds
                }
            });
        }

        private void StartTcpServer()
        {
            tcp = new TcpListener(IPAddress.Any, tcpPort);
            tcp.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await tcp.AcceptTcpClientAsync();
                    _ = HandleClient(client);
                }
            });
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new BinaryReader(stream);

            var nameLen = reader.ReadInt32();
            var fileName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));

            var fileLen = reader.ReadInt32();
            var fileBytes = reader.ReadBytes(fileLen);

            var dir = _contentFolder;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            await File.WriteAllBytesAsync(path, fileBytes);

            FileReceived?.Invoke(path);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
