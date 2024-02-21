using hidsynth;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using Topten.WindowsAPI;

// Base file name for recording
string outBase;

// The set of recorded or loaded events
InputEvents events = new();

if (args.Length > 0)
{
    // Read events from command line specified input file
    var filename = args[0];
    outBase = System.IO.Path.GetFileNameWithoutExtension(filename);
    events.Load(filename);
}
else
{
    // Record events
    outBase = $"hidsynth-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
    InputHook hook = new InputHook();
    hook.OnEvent = (e) => events.Add(e);
    hook.Run();
    events.Save(outBase + ".txt");
}

// Load sample set
var sampleSet = new SampleSet();
sampleSet.LoadFromResource();

// Render
var renderer = new Renderer(sampleSet);
renderer.Render(events, $"{outBase}.wav");

return 0;


