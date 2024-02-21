
namespace hidsynth
{
    public class WaveReader : IDisposable
    {
        public WaveReader(string filename)
        {
            Init(new FileStream(filename, FileMode.Open, FileAccess.Read));
        }

        public WaveReader(Stream stream)
        {
            Init(stream);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            if (_fs != null)
            {
                _fs.Close();
                _fs.Dispose();
                _fs = null;
            }
        }

        public void Init(Stream stream)
        {
            try
            {
                // Open the stream
                _fs = stream;

                // Read the header chunk
                RIFFCHUNK chunk = new RIFFCHUNK();
                chunk.Read(_fs);
                if (chunk.chunkID != WAVEFORMATINFO.CHUNKID_RIFF)
                    throw new InvalidDataException("Not a wave file");

                // Check the riff type
                uint riffType = StreamUtils.ReadUInt(_fs);
                if (riffType != WAVEFORMATINFO.RIFFTYPE_WAVE)
                    throw new InvalidDataException("Not a wave file");

                bool headerRead = false;
                while (true)
                {
                    // Read the next chunk header
                    chunk.Read(_fs);

                    // Data chunk?
                    if (chunk.chunkID == WAVEFORMATINFO.WAVE_CHUNKID_DATA)
                    {
                        if (!headerRead)
                            throw new InvalidDataException("Corrupt wave file (data chunk found without format info)");

                        // Calculate the total number of samples
                        _totalSamples = (chunk.length / _bytesPerSample) / _channels;
                        _firstSampleOffset = _fs.Position;
                        return;
                    }

                    // Other chunk,
                    if (chunk.chunkID == WAVEFORMATINFO.WAVE_CHUNKID_FMT)
                    {
                        headerRead = true;
                        if (chunk.length > 4096)
                            throw new InvalidDataException("Corrupt wave file (format chunk too long)");

                        // Work out where the chunk ends
                        long endOfChunk = _fs.Position + chunk.length;

                        // Read the wave format
                        _format.Read(_fs);

                        if (_format.wFormatTag != WAVEFORMATINFO.WAVE_FORMAT_PCM)
                            throw new InvalidDataException("Unsupported file format (not PCM)");

                        _channels = _format.wChannels;
                        _sampleRate = (int)_format.dwSamplesPerSec;
                        _bytesPerSample = (uint)(_format.wBitsPerSample / 8);
                        switch (_bytesPerSample)
                        {
                            case 1:
                                _readAndConvertSample = () => (((float)_fs.ReadByte()) - 128.0f) / 127.49999f;
                                break;

                            case 2:
                                _readAndConvertSample = () => ((float)StreamUtils.ReadShort(_fs)) / 32767.49999f;
                                break;

                            case 4:
                                _readAndConvertSample = () => ((float)StreamUtils.ReadInt(_fs)) / 2147483647.49999f;
                                break;

                            default:
                                throw new InvalidDataException("Unsupported file format (bytes per sample)");
                        }

                        // Seek to end of chunk
                        _fs.Seek(endOfChunk, SeekOrigin.Begin);
                        continue;
                    }


                    // Unknown chunk, skip it
                    _fs.Seek(chunk.length, SeekOrigin.Current);
                }
            }
            catch (System.Exception)
            {
                if (_fs!=null)
                    _fs.Close();
                throw;
            }
            

        }

        public float ReadSample()
        {
            return _readAndConvertSample();
        }

        public long CurrentSample
        {
            get
            {
                return (_fs.Position - _firstSampleOffset) / (_bytesPerSample * _channels);
            }
            set
            {
                _fs.Seek(_firstSampleOffset + value * _channels * _bytesPerSample, SeekOrigin.Begin);
            }
        }

        public bool EOF
        {
            get
            {
                return CurrentSample >= _totalSamples;
            }
        }

        public int Channels
        {
            get
            {
                return (int)_channels;  
            }
        }

        public long TotalSamples
        {
            get
            {
                return _totalSamples;
            }
        }

        public int SampleRate
        {
            get
            {
                return _sampleRate;
            }
        }



        Stream _fs;
        WAVEFORMATINFO _format;
        long _totalSamples;
        uint _channels;
        int _sampleRate;
        uint _bytesPerSample;
        long _firstSampleOffset;
        Func<float> _readAndConvertSample;
    }
}
