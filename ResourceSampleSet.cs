using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hidsynth
{

    internal class ResourceSampleSet : ISampleSet
    {
        public ResourceSampleSet()
        {
        }
        
        public int SampleRate { get; private set; }

        Dictionary<string, float[]> samples = new();

        public float[] GetSamples(int eventId, bool press)
        {
            string sampleName = null;
            switch (eventId)
            {
                case -1:
                case -2:
                case -3:
                    sampleName = press ? "lmouse_down" : "lmouse_up";
                    break;
            }

            // Default key?
            if (sampleName == null)
                sampleName = press ? "normal_press" : "normal_release";

            return samples[sampleName];
        }

        public void LoadFromResource()
        {
            // Load all samples
            var names = typeof(Program).Assembly.GetManifestResourceNames();
            foreach (var n in names.Where(x => x.StartsWith("hidsynth.samples2.") && x.EndsWith(".wav")))
            {
                var shortName = n.Substring(18, n.Length - 22);

                var stream = typeof(Program).Assembly.GetManifestResourceStream(n);
                using (var reader = new WaveReader(stream))
                {
                    if (SampleRate == 0)
                        SampleRate = reader.SampleRate;
                    else if (SampleRate != reader.SampleRate)
                        throw new InvalidDataException("All samples must have the same sample rate");

                    var totalSamples = reader.TotalSamples;
                    var sampleBuf = new float[totalSamples];
                    for (int i = 0; i < totalSamples; i++)
                    {
                        sampleBuf[i++] = reader.ReadSample();
                    }
                    samples.Add(shortName, sampleBuf);
                }
            }
        }


    }
}
