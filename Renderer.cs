using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hidsynth
{
    internal class Renderer
    {
        public Renderer(ISampleSet sampleSet)
        {
            _sampleSet = sampleSet;
        }

        ISampleSet _sampleSet;

        public void Render(InputEvents events, string filename)
        {
            int nextEvent = 0;
            List<Voice> activeVoices = new();
            uint currentSample = 0;
            var r = new Random();

            using (var wr = new WaveWriter(filename, _sampleSet.SampleRate))
            {
                float[] buf = new float[512];
                while (nextEvent < events.Count || activeVoices.Count > 0)
                {
                    // Clear buffer
                    Array.Clear(buf);

                    // Create new voices
                    while (nextEvent < events.Count)
                    {
                        var e = events[0];
                        uint startSample = microsToSamples(e.timeStamp);
                        if (startSample < currentSample + buf.Length)
                        {
                            uint offset = startSample - currentSample;
                            var samples = _sampleSet.GetSamples(e.eventId, e.press);

                            var gain = 1.0f;
                            if (e.press)
                                gain = (float)(DbToScalar(-r.NextDouble() * 45));
                            var voice = new Voice(samples, startSample - currentSample, gain);
                            activeVoices.Add(voice);
                            events.RemoveAt(0);
                        }
                        else
                            break;
                    }

                    // Render active voices
                    for (int i = activeVoices.Count - 1; i >= 0; i--)
                    {
                        if (!activeVoices[i].Render(buf))
                            activeVoices.RemoveAt(i);
                    }

                    // Write samples
                    for (int i = 0; i < buf.Length; i++)
                    {
                        wr.WriteSample(buf[i]);
                    }

                    // Update current sample position
                    currentSample += (uint)buf.Length;
                }
            }

            Console.WriteLine($"Rendered {(double)currentSample / _sampleSet.SampleRate:N2} seconds at {_sampleSet.SampleRate}Hz");
        }


        uint microsToSamples(long millis)
        {
            return (uint)(((ulong)millis * (ulong)_sampleSet.SampleRate) / 1000000);
        }


        double DbToScalar(double db)
        {
            if (db <= -100)
                return 0;
            return Math.Pow(10.0, db / 20.0);
        }


        class Voice
        {
            public Voice(float[] samples, uint startOffset, float gain)
            {
                _samples = samples;
                _startOffset = startOffset;
                _position = 0;
                _gain = gain;
            }

            public bool Render(float[] buffer)
            {
                if (_position >= _samples.Length)
                    return false;

                // Work out how many samples to mix in
                uint samplesThisCycle = (uint)buffer.Length - _startOffset;
                if (_position + samplesThisCycle > _samples.Length)
                    samplesThisCycle = (uint)_samples.Length - _position;

                // Mix
                for (int i = 0; i < samplesThisCycle; i++)
                {
                    buffer[_startOffset + i] += _samples[_position + i] * _gain;
                }

                // Update position
                _position += samplesThisCycle;
                _startOffset = 0;

                return _position < _samples.Length;
            }

            float[] _samples;
            float _gain;
            uint _position;
            uint _startOffset;

        }
    }
}
