
namespace hidsynth
{
    public class WaveWriter : IDisposable
    {
        public WaveWriter(string filename, int sampleRate)
        {
            Init(new FileStream(filename, FileMode.Create, FileAccess.ReadWrite), sampleRate);
        }
        public WaveWriter(Stream stream, int sampleRate)
        {
            Init(stream, sampleRate);
        }

        void Init(Stream stream, int sampleRate)
        {
            fs = stream;

            // Write header placeholder
            offsetChunkRiff = fs.Position;
            chunkRiff = new RIFFCHUNK();
            chunkRiff.chunkID = WAVEFORMATINFO.CHUNKID_RIFF;
            chunkRiff.Write(fs);

            // Write the riff type
            StreamUtils.WriteUInt(fs, WAVEFORMATINFO.RIFFTYPE_WAVE);

            // Write the format chunk 
            offsetChunkFormat = fs.Position;
            chunkFmt = new RIFFCHUNK();
            chunkFmt.chunkID = WAVEFORMATINFO.WAVE_CHUNKID_FMT;
            chunkFmt.Write(fs);

            // And the format 
            var fmt = new WAVEFORMATINFO();
            fmt.wBitsPerSample = 16;
            fmt.wBlockAlign = 1;
            fmt.wChannels = 1;
            fmt.wFormatTag = WAVEFORMATINFO.WAVE_FORMAT_PCM;
            fmt.dwSamplesPerSec = (uint)sampleRate;
            fmt.dwAvgBytesPerSec = (uint)sampleRate;
            fmt.Write(fs);

            // Write the data header
            offsetData = fs.Position;
            chunkData = new RIFFCHUNK();
            chunkData.chunkID = WAVEFORMATINFO.WAVE_CHUNKID_DATA;
        }

        Stream fs;
        long offsetChunkRiff;
        long offsetChunkFormat;
        RIFFCHUNK chunkRiff;
        RIFFCHUNK chunkFmt;
        RIFFCHUNK chunkData;


        long offsetData;

        public void WriteSample(float value)
        {
            StreamUtils.WriteShort(fs, (short)(value * 32767.0));
        }


        public void Close()
        {
            if (fs == null)
                return;

            // Remember the total length
            long totalLength = fs.Position;

            // Go back an fix up the chunk headers
            fs.Seek(offsetChunkRiff, SeekOrigin.Begin);
            chunkRiff.length = (uint)(totalLength - 8);
            chunkRiff.Write(fs);

            fs.Seek(offsetChunkFormat, SeekOrigin.Begin);
            chunkFmt.length = (uint)(offsetData - offsetChunkFormat - 8);
            chunkFmt.Write(fs);

            fs.Seek(offsetData, SeekOrigin.Begin);
            chunkData.length = (uint)(totalLength - offsetData - 8);
            chunkData.Write(fs);

            if (fs!= null)
            {
                fs.Dispose();
                fs = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
