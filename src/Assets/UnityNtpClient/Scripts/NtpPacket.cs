using System;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable BuiltInTypeReferenceStyle

namespace HassakuLab.NtpClients
{
    /// <summary>
    /// Packet for NTP
    /// </summary>
    public class NtpPacket
    {
        private const int PacketSize = 48;
        private readonly byte[] bytes = new byte[PacketSize];

        /// <summary>
        /// create empty packet
        /// </summary>
        public NtpPacket(){}
        
        /// <summary>
        /// create packet from received
        /// </summary>
        /// <param name="packet">received packet</param>
        public NtpPacket(byte[] packet)
        {
            Array.Copy(packet, bytes, PacketSize);
        }

        /// <summary>
        /// Get packet byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return bytes;
        }

        /// <summary>
        /// Leap Seconds enumeration
        /// </summary>
        public enum LeapIndicatorEnum
        {
            NoIndicate = 0,
            LastMinuteIs61Sec = 1,
            LastMinuteIs59Sec = 2,
            Unknown = 3,
        }

        /// <summary>
        /// get Leap Indicator
        /// </summary>
        public LeapIndicatorEnum LeapIndicator
        {
            get
            {
                int leap = (bytes[0] >> 6) & 0b_00000011;
                return (LeapIndicatorEnum) leap;
            }
        }
        

        /// <summary>
        /// NTP Version (ordinary is 4)
        /// </summary>
        public int VersionNumber
        {
            get => (bytes[0] >> 3) & 0b_00000111;
            set
            {
                bytes[0] &= 0b_11000111;
                bytes[0] |= (byte)((value & 0b_00000111) << 3);
            }
        }

        /// <summary>
        /// Mode Enumeration
        /// </summary>
        public enum ModeEnum
        {
            Reserved = 0,
            SymmetricActive = 1,
            SymmetricPassive = 2,
            Client = 3,
            Server = 4,
            Broadcast = 5,
            NTPControlMessage = 6,
            Private = 7,
        }

        /// <summary>
        /// Association Mode
        /// </summary>
        public ModeEnum Mode
        {
            get
            {
                int leap = bytes[0] & 0b_00000111;
                return (ModeEnum) leap;
            }
            set
            {
                int mode = (int) value;
                bytes[0] &= 0b_11111000;
                bytes[0] |= (byte) (mode & 0b_00000111);
            }
        }

        /// <summary>
        /// get Stratum
        /// </summary>
        public byte Stratum => bytes[1];

        /// <summary>
        /// get Poll
        /// </summary>
        public byte Poll => bytes[2];

        /// <summary>
        /// get Precision
        /// </summary>
        public byte Precision => bytes[3];

        /// <summary>
        /// get Root Delay
        /// </summary>
        public UInt32 RootDelay => GetInt32(4);

        /// <summary>
        /// get Root Dispersion
        /// </summary>
        public UInt32 RootDispersion => GetInt32(8);

        /// <summary>
        /// get Reference ID
        /// </summary>
        public UInt32 ReferenceId => GetInt32(12);

        /// <summary>
        /// Last synced timestamp
        /// </summary>
        private UInt64 ReferenceTimestamp => GetInt64(16);

        /// <summary>
        /// Transmit Timestamp of last received packet (t1)
        /// </summary>
        private UInt64 OriginTimestamp => GetInt64(24);

        /// <summary>
        /// server timestamp when receive packet (t2)
        /// </summary>
        private UInt64 ReceiveTimestamp => GetInt64(32);

        /// <summary>
        /// server timestamp when send packet (t3)
        /// </summary>
        private UInt64 TransmitTimestamp
        {
            get => GetInt64(40);
            set => SetInt64(40, value);
        }

        /// <summary>
        /// set timestamp when packet send
        /// </summary>
        public DateTimeOffset TransmitTimestampDateTime
        {
            get => TimestampToDateTime(TransmitTimestamp);
            set => TransmitTimestamp = DateTimeToTimestamp(value);
        }

        /// <summary>
        /// calculate client offset (client time + offset = realtime)
        /// </summary>
        /// <param name="clientReceive">receive timestamp (t4)</param>
        /// <returns></returns>
        public TimeSpan CalcClientOffset(DateTimeOffset clientReceive)
        {
            //                      (t3 + t2) - (t1 + t4)
            //  client offset   =   ---------------------
            //                                2
            //
            //                  =   (t3 - t1) / 2 + (t2 - t4) / 2

            UInt64 t1 = OriginTimestamp;
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
        private UInt64 GetInt64(int offset)
        {
            UInt64 result = 0;

            //  HACK:   unroll if you want to optimize tightly
            for (int i = 0; i < 8; ++i)
            {
                result <<= 8;
                result |= bytes[offset + i];
            }

            return result;
        }

        /// <summary>
        /// Set 64bit integer to byte array
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        private void SetInt64(int offset, UInt64 value)
        {
            //  HACK:   unroll if you want to optimize tightly
            for (int i = 0; i < 8; ++i)
            {
                bytes[offset + i] = (byte) ((value >> ((7 - i) * 8)) & 0b_11111111);
            }
        }
        
        /// <summary>
        /// Get 32bit integer from byte array
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private UInt32 GetInt32(int offset)
        {
            UInt32 result = 0;

            //  HACK:   unroll if you want to optimize tightly
            for (int i = 0; i < 4; ++i)
            {
                result <<= 8;
                result |= bytes[offset + i];
            }

            return result;
        }

        //  1900/1/1
        private static readonly DateTimeOffset
            UnixTimeOffset = new DateTimeOffset(1900, 1, 1, 0, 0, 0, new TimeSpan(0));

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