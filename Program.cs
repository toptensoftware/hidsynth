using hidsynth;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using Topten.WindowsAPI;

string outBase;
List<EVENT> events = new();
HashSet<int> heldKeys = new HashSet<int>();
IntPtr keyboardHook = IntPtr.Zero;
IntPtr mouseHook = IntPtr.Zero;
Stopwatch sw = new Stopwatch();
sw.Start();
long startTime = GetTimeStamp();

if (args.Length > 0)
{
    var filename = args[0];
    outBase = System.IO.Path.GetFileNameWithoutExtension(filename);
    foreach (var l in System.IO.File.ReadAllLines(filename))
    {
        var parts = l.Split(',');
        if (parts.Length == 3)
        {
            var e = new EVENT()
            {
                timeStamp = long.Parse(parts[0]),
                eventId = int.Parse(parts[1]),
                press = parts[2] == "True",
            };
            events.Add(e);
        }
    }
}
else
{
    // Install keyboard hook
    keyboardHook = WinApi.SetWindowsHookEx(WinApi.WH_KEYBOARD_LL, KeyboardHookProc, IntPtr.Zero, 0);
    mouseHook = WinApi.SetWindowsHookEx(WinApi.WH_MOUSE_LL, MouseHookProc, IntPtr.Zero, 0);

    uint mainThreadId = WinApi.GetCurrentThreadId();

    // Quit on Ctrl+C
    Console.CancelKeyPress += (sender, args) =>
    {
        Console.WriteLine("\nCancelling...\n");
        WinApi.PostThreadMessage(mainThreadId, WinApi.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        args.Cancel = true;
    };

    // Message loop
    WinApi.MSG msg;
    while (WinApi.GetMessage(out msg, IntPtr.Zero, 0, 0))
    {
        WinApi.DispatchMessage(ref msg);
    }

    // Clean up
    WinApi.UnhookWindowsHookEx(keyboardHook);
    WinApi.UnhookWindowsHookEx(mouseHook);
    keyboardHook = IntPtr.Zero;
    mouseHook = IntPtr.Zero;

    var now = DateTime.Now;
    outBase = $"hidsynth-{now:yyyy-MM-dd-HH-mm-ss}";

    System.IO.File.WriteAllText(outBase + ".txt", string.Join("\n",
        events.Select(x => $"{x.timeStamp},{x.eventId},{x.press}")
    ));
}

// Load all samples
int sampleRate = 0;
Dictionary<string, float[]> samples = new();
var names = typeof(Program).Assembly.GetManifestResourceNames();
foreach (var n in names.Where(x => x.StartsWith("hidsynth.samples2.") && x.EndsWith(".wav")))
{
    var shortName = n.Substring(18, n.Length - 22);

    var stream = typeof(Program).Assembly.GetManifestResourceStream(n);
    using (var reader = new WaveReader(stream))
    {
        if (sampleRate == 0)
            sampleRate = reader.SampleRate;
        else if (sampleRate != reader.SampleRate)
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


int nextEvent = 0;
List<Voice> activeVoices = new();
uint currentSample = 0;
var r = new Random();
using (var wr = new WaveWriter($"{outBase}.wav", sampleRate))
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
                string sampleName = null;
                switch (e.eventId)
                {
                    case -1:
                        sampleName = e.press ? "lmouse_down" : "lmouse_up";
                        break;

                    case -2:
                    case -3:
                        sampleName = e.press ? "rmouse_down" : "rmouse_up";
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
                    sampleName = e.press ? "normal_press_1" : "normal_release";


                var gain = 1.0f;
                if (e.press)
                    gain = (float)(DbToScalar(-r.NextDouble() * 45));
                var voice = new Voice(samples[sampleName], startSample - currentSample, gain);
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

Console.WriteLine($"Rendered {(double)currentSample/sampleRate:N2} seconds at {sampleRate}Hz");



return 0;

uint microsToSamples(long millis)
{
    return (uint)(((ulong)millis * (ulong)sampleRate) / 1000000);
}

void OnEvent(int eventId, bool press)
{
    events.Add(new EVENT()
    {
        timeStamp = GetTimeStamp() - startTime,
        eventId = eventId,
        press = press,
    });
    Console.Write($"{events.Count} events\r");
}


IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
{
    if (code >= 0)
    {
        var kbd = Marshal.PtrToStructure<WinApi.KBDLLHOOKSTRUCT>(lParam);
        var scanCode = (int)kbd.scanCode;
        var isPress = (kbd.flags & 0x80) == 0;

        if (isPress)
        {
            if (heldKeys.Contains(scanCode))
                goto exit;
            heldKeys.Add(scanCode);
        }
        else
        {
            heldKeys.Remove(scanCode);
        }

        OnEvent(scanCode, isPress);
    }

exit:
    return WinApi.CallNextHookEx(keyboardHook, code, wParam, lParam);
}

IntPtr MouseHookProc(int code, IntPtr wParam, IntPtr lParam)
{
    if (code >= 0)
    {
        switch ((uint)wParam.ToInt32())
        {
            case WinApi.WM_LBUTTONDOWN:
                OnEvent(-1, true);
                break;

            case WinApi.WM_RBUTTONDOWN:
                OnEvent(-2, true);
                break;

            case WinApi.WM_MBUTTONDOWN:
                OnEvent(-3, true);
                break;

            case WinApi.WM_LBUTTONUP:
                OnEvent(-1, false);
                break;

            case WinApi.WM_RBUTTONUP:
                OnEvent(-2, false);
                break;

            case WinApi.WM_MBUTTONUP:
                OnEvent(-3, false);
                break;
        }
    }
    return WinApi.CallNextHookEx(keyboardHook, code, wParam, lParam);
}


double DbToScalar(double db)
{
    if (db <= -100)
        return 0;
    return Math.Pow(10.0, db / 20.0);
}

long GetTimeStamp()
{
    return sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;
}


struct EVENT
{
    public long timeStamp;
    public int eventId;
    public bool press;
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

