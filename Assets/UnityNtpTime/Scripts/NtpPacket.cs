using System;
using System.Runtime.InteropServices;
using UnityEngine;

// ReSharper disable BuiltInTypeReferenceStyle

namespace HassakuLab.NtpTimes
{
    /// <summary>
    /// Packet for NTP (implements only required)
    /// </summary>
    public class NtpPacket
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DataStructure
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] bytes;
        }
        
        private readonly byte[] packet;
        
        /// <summary>
        /// create packet from received
        /// </summary>
        /// <param name="packet">received packet</param>
        public NtpPacket(byte[] packet)
        {
            //  HACK:   replace to deep copy if needed.
            this.packet = packet;
        }

        /// <summary>
        /// server timestamp when receive packet (t2)
        /// </summary>
        private UInt64 ReceiveTimestamp => Get64(32);

        /// <summary>
        /// server timestamp when send packet (t3)
        /// </summary>
        private UInt64 TransmitTimestamp => Get64(40);

        /// <summary>
        /// calculate client offset
        /// </summary>
        /// <param name="clientTransmit">transmit timestamp (t1)</param>
        /// <param name="clientReceive">receive timestamp (t2)</param>
        /// <returns></returns>
        public TimeSpan GetClientOffset(DateTimeOffset clientTransmit, DateTimeOffset clientReceive)
        {
            //  client offset: ((t3 + t2) - (t1 + t4)) / 2
            //  => (t3 - t1) / 2 + (t2 - t4) / 2

            UInt64 t1 = DateTimeToTimestamp(clientTransmit);
            UInt64 t2 = ReceiveTimestamp;
            UInt64 t3 = TransmitTimestamp;
            UInt64 t4 = DateTimeToTimestamp(clientReceive);

            UInt64 offset = (t3 - t1) / 2  + (t2 - t4) / 2;
            return TimeSpan.FromSeconds(offset * Math.Pow(2, -32));
        }
        
        /// <summary>
        /// Get 64bit integer from byte array
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private UInt64 Get64(int offset)
        {
            UInt64 result = 0;

            //  HACK:   unroll if you want to optimize tightly
            for (int i = 0; i < 8; ++i)
            {
                result <<= 8;
                result |= packet[offset + i];
            }

            return result;
        }

        //  1990/1/1
        private static readonly DateTimeOffset
            UnixTimeOffset = new DateTimeOffset(1990, 1, 1, 0, 0, 0, new TimeSpan(0));

        /// <summary>
        /// convert NTP timestamp to DateTime
        /// </summary>
        /// <param name="timestamp">NTP timestamp</param>
        /// <returns>DateTime typed timestamp</returns>
        private static DateTimeOffset TimestampToDateTime(UInt64 timestamp)
        {
            return UnixTimeOffset.AddSeconds(timestamp * Math.Pow(2, -32));
        }
        
        /// <summary>
        /// convert DateTime to NTP timestamp
        /// </summary>
        /// <param name="timestamp">DateTime typed timestamp</param>
        /// <returns>UInt64 timestamp</returns>
        private static UInt64 DateTimeToTimestamp(DateTimeOffset timestamp)
        {
            return (UInt64) ((timestamp - UnixTimeOffset).TotalSeconds / Math.Pow(2, -32));
        }
    }
}