using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

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
        public static async Task<DateTime> Request(string serverAddress, int timeoutSec)
        {
            using UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, NtpPort));
            IPEndPoint remoteEndpoint = null;
                
            if (timeoutSec > 0)
            {
                int timeoutMilli = timeoutSec * 1000;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeoutMilli);
            }
                
            byte[] packet = new byte[PacketByteLength];
            packet[0] = 0xB;    //  MODE 011 = Client
                
            //  transmit
            DateTime transmitTime = DateTime.Now;
            await client.SendAsync(packet, PacketByteLength, serverAddress, NtpPort);
            DateTime receiveTime = DateTime.Now;
            
            NtpPacket receivePacket = new NtpPacket(client.Receive(ref remoteEndpoint));
            TimeSpan offset = receivePacket.GetClientOffset(transmitTime, receiveTime);

            return DateTime.Now + offset;
        }
    }
}