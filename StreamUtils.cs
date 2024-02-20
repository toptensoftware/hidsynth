namespace hidsynth
{
    class StreamUtils
    {
        public static uint ReadUInt(Stream stream)
        {
            unchecked
            {
                uint a = (uint)stream.ReadByte();
                uint b = (uint)stream.ReadByte();
                uint c = (uint)stream.ReadByte();
                uint d = (uint)stream.ReadByte();
                return a | (b << 8) | (c << 16) | (d << 24);
            }
        }

        public static void WriteUInt(Stream stream, uint value)
        {
            unchecked
            {
                stream.WriteByte((byte)((value >> 0) & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
                stream.WriteByte((byte)((value >> 16) & 0xFF));
                stream.WriteByte((byte)((value >> 24) & 0xFF));
            }
        }

        public static void WriteUShort(Stream stream, ushort value)
        {
            unchecked
            {
                stream.WriteByte((byte)((value >> 0) & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
            }
        }

        public static void WriteShort(Stream stream, short value)
        {
            unchecked
            {
                WriteUShort(stream, (ushort)value);
            }
        }

        public static ushort ReadUShort(Stream stream)
        {
            unchecked
            {
                ushort a = (ushort)stream.ReadByte();
                ushort b = (ushort)stream.ReadByte();
                return (ushort)(a | (b << 8));
            }
        }

        public static short ReadShort(Stream stream)
        {
            unchecked
            {
                return (short)ReadUShort(stream);
            }
        }
        public static int ReadInt(Stream stream)
        {
            unchecked
            {
                return (int)ReadUInt(stream);
            }
        }
    }

}
