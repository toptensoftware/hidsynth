namespace hidsynth
{
    internal interface ISampleSet
    {
        int SampleRate { get; }
        float[] GetSamples(int eventId, bool press);
    }
}
