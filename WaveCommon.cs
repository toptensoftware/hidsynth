
namespace hidsynth
{
    struct RIFFCHUNK
    {
        public uint chunkID;
        public uint length;

        public void Read(Stream stream)
        {
            chunkID = StreamUtils.ReadUInt(stream);
            length = StreamUtils.ReadUInt(stream);
        }

        public void Write(Stream stream)
        {
            StreamUtils.WriteUInt(stream, chunkID);
            StreamUtils.WriteUInt(stream, length);
        }
    }

    struct WAVEFORMATINFO
    {
        public ushort wFormatTag;
        public ushort wChannels;
        public uint dwSamplesPerSec;
        public uint dwAvgBytesPerSec;
        public ushort wBlockAlign;
        public ushort wBitsPerSample;

        public void Read(Stream stream)
        {
            wFormatTag = StreamUtils.ReadUShort(stream);
            wChannels = StreamUtils.ReadUShort(stream);
            dwSamplesPerSec = StreamUtils.ReadUInt(stream);
            dwAvgBytesPerSec = StreamUtils.ReadUInt(stream);
            wBlockAlign = StreamUtils.ReadUShort(stream);
            wBitsPerSample = StreamUtils.ReadUShort(stream);
        }

        public void Write(Stream stream)
        {
            StreamUtils.WriteUShort(stream, wFormatTag);
            StreamUtils.WriteUShort(stream, wChannels);
            StreamUtils.WriteUInt(stream, dwSamplesPerSec);
            StreamUtils.WriteUInt(stream, dwAvgBytesPerSec);
            StreamUtils.WriteUShort(stream, wBlockAlign);
            StreamUtils.WriteUShort(stream, wBitsPerSample);
        }

        public const uint CHUNKID_RIFF = 0x46464952;		//'RIFF'
        public const uint RIFFTYPE_WAVE = 0x45564157;
        public const uint WAVE_CHUNKID_FMT = 0x20746d66;	//'fmt '
        public const uint WAVE_CHUNKID_DATA = 0x61746164;	//'data'
        public const uint WAVE_CHUNKID_JUNK = 0x6b6e756a;	//'junk'
        public const int WAVE_FORMAT_PCM = 1;
    };

}
