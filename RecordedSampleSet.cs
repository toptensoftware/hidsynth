using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hidsynth
{

    internal class RecordedSampleSet : ISampleSet
    {
        public RecordedSampleSet(string waveFile, string eventFile, long offset)
        {
            // Read input Events
            var events = new InputEvents();
            events.Load(eventFile, offset);

            // Open wave file
            using (var stream = new FileStream(waveFile, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new WaveReader(stream))
                {
                     
                }
            }
        }

        public int SampleRate { get; private set; }

        public float[] GetSamples(int eventId, bool press)
        {
            throw new NotImplementedException();
        }
    }
}
