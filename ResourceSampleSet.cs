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
                    sampleName = press ? "lmouse_down" : "lmouse_up";
                    break;

                case -2:
                case -3:
                    sampleName = press ? "rmouse_down" : "rmouse_up";
                    break;

                    /*
                case 14: // backspace
                    sampleName = e.press ? "backspace_press_1" : "backspace_release";
                    break;

                case 1: // escape
                    sampleName = e.press ? "escape_press_1" : "escape_release";
                    break;

                case 57: // space
                    sampleName = e.press ? "space_press_1" : "normal_release";
                    break;

                case 42:
                case 54:
                case 29:
                    // shift/ctrl/alt
                    sampleName = e.press ? "shift_press_2" : "normal_release";
                    break;


                case 28:
                    // enter
                    sampleName = e.press ? "normal_press_1" : "enter_release";
                    break;
                    */



            }

            // Default key?
            if (sampleName == null)
                sampleName = press ? "normal_press_1" : "normal_release";

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
