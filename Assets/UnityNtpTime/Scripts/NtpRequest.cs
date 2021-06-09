using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HassakuLab.NtpTimes
{
    /// <summary>
    /// request sync time
    /// </summary>
    static class NtpRequest
    {
        private const int NtpPort = 123;
        private const int PacketByteLength = 48;

        /// <summary>
        /// send NTP Packet
        /// </summary>
        /// <param name="serverAddress">NTP Server Address</param>
        /// <param name="timeoutSec">timeout (sec)</param>
        /// <returns>Now time</returns>
        public static async Task<DateTimeOffset> Request(string serverAddress, int timeoutSec)
        {
            using UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, NtpPort));
            IPEndPoint remoteEndpoint = null;
                
            if (timeoutSec > 0)
            {
                int timeoutMilli = timeoutSec * 1000;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeoutMilli);
            }

            NtpPacket packet = new NtpPacket()
            {
                VersionNumber = 4,
                Mode = NtpPacket.ModeEnum.Client,
                TransmitTimestampDateTime = DateTimeOffset.Now,
            };
                
            //  transmit
            await client.SendAsync(packet.ToByteArray(), PacketByteLength, serverAddress, NtpPort);
            DateTimeOffset receiveTime = DateTimeOffset.Now;
            
            NtpPacket receivePacket = new NtpPacket(client.Receive(ref remoteEndpoint));
            TimeSpan offset = receivePacket.CalcClientOffset(receiveTime);

            return DateTimeOffset.Now + offset;
        }
    }
}