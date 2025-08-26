using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GrowJoMobileImageSender.Utilities
{
    public class LanSender
    {
        private int udpPort = 5051;
        private int timeout = 2000; // 2 sec

        public async Task<(string ip, int port)?> DiscoverAsync()
        {
            using var udp = new UdpClient(udpPort);
            udp.Client.ReceiveTimeout = timeout;
            var endpoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                var result = await udp.ReceiveAsync();
                var msg = Encoding.UTF8.GetString(result.Buffer);
                if (msg.StartsWith("GROJORECEIVER:"))
                {
                    var parts = msg.Split(':');
                    return (parts[1], int.Parse(parts[2]));
                }
            }
            catch { }
            return null;
        }

        public async Task SendFileAsync(string ip, int port, string filePath)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);
            using var stream = client.GetStream();
            using var writer = new BinaryWriter(stream);

            var fileName = Path.GetFileName(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var nameBytes = Encoding.UTF8.GetBytes(fileName);

            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(fileBytes.Length);
            writer.Write(fileBytes);
        }
    }
}
