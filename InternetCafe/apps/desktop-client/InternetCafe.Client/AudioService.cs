using System.IO;
using System.Media;

namespace InternetCafe.Client;

public static class AudioService
{
    private static readonly Dictionary<string, SoundPlayer> Players = new(StringComparer.OrdinalIgnoreCase);
    private static bool initialized;
    private static readonly object LockObj = new();

    public static bool IsMuted { get; set; }

    public static void Initialize()
    {
        if (initialized) return;
        lock (LockObj)
        {
            if (initialized) return;
            try
            {
                var soundsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
                Directory.CreateDirectory(soundsDir);

                EnsureWav(Path.Combine(soundsDir, "login.wav"), GenerateLoginSound);
                EnsureWav(Path.Combine(soundsDir, "order.wav"), GenerateOrderSound);
                EnsureWav(Path.Combine(soundsDir, "topup.wav"), GenerateTopupSound);
                EnsureWav(Path.Combine(soundsDir, "click.wav"), GenerateClickSound);

                foreach (var file in Directory.GetFiles(soundsDir, "*.wav"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        var player = new SoundPlayer(file);
                        player.Load();
                        Players[name] = player;
                    }
                    catch
                    {
                        // ignore corrupt or locked audio
                    }
                }
            }
            catch
            {
                // ignore audio init errors in headless/restricted envs
            }
            finally
            {
                initialized = true;
            }
        }
    }

    public static void Play(string soundName)
    {
        if (IsMuted) return;
        try
        {
            if (!initialized) Initialize();
            if (Players.TryGetValue(soundName, out var player))
            {
                player.Play();
            }
        }
        catch
        {
            // audio playback error should never crash the app
        }
    }

    private static void EnsureWav(string path, Func<byte[]> generator)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            var bytes = generator();
            File.WriteAllBytes(path, bytes);
        }
    }

    // 1. Futuristic Cyber Power-On sound: sub bass sweep + harmonic chime chord
    private static byte[] GenerateLoginSound()
    {
        const int sampleRate = 44100;
        const double duration = 1.25;
        var samples = new short[(int)(sampleRate * duration)];

        for (int i = 0; i < samples.Length; i++)
        {
            var t = (double)i / sampleRate;

            // Sub bass frequency sweep: 85Hz -> 220Hz
            var bassFreq = 85.0 + 135.0 * Math.Pow(t / duration, 0.7);
            var bass = Math.Sin(2.0 * Math.PI * bassFreq * t) * Math.Exp(-2.2 * t);

            // Shimmering chime chord (C5=523Hz, E5=659Hz, G5=784Hz, C6=1046Hz) blooming at 0.15s
            var chimeEnv = t < 0.15 ? t / 0.15 : Math.Exp(-3.2 * (t - 0.15));
            var chime = 0.25 * Math.Sin(2.0 * Math.PI * 523.25 * t)
                      + 0.22 * Math.Sin(2.0 * Math.PI * 659.25 * t)
                      + 0.20 * Math.Sin(2.0 * Math.PI * 783.99 * t)
                      + 0.25 * Math.Sin(2.0 * Math.PI * 1046.50 * t);

            var sample = (bass * 0.45 + chime * chimeEnv * 0.55);
            samples[i] = (short)Math.Clamp(sample * short.MaxValue * 0.85, short.MinValue, short.MaxValue);
        }

        return CreateWavPcm16(samples, sampleRate);
    }

    // 2. Order Success: Metallic Cash Register "Cha-Ching" + upward energetic chime
    private static byte[] GenerateOrderSound()
    {
        const int sampleRate = 44100;
        const double duration = 0.75;
        var samples = new short[(int)(sampleRate * duration)];

        for (int i = 0; i < samples.Length; i++)
        {
            var t = (double)i / sampleRate;
            double sample = 0;

            // Click 1 (0 to 0.08s)
            if (t < 0.08)
            {
                var env = Math.Exp(-45.0 * t);
                sample += 0.4 * Math.Sin(2.0 * Math.PI * 2800 * t) * env;
            }

            // Click 2 / Coin clink (0.05s onwards)
            if (t >= 0.05 && t < 0.35)
            {
                var dt = t - 0.05;
                var env = Math.Exp(-25.0 * dt);
                sample += 0.45 * (Math.Sin(2.0 * Math.PI * 3400 * dt) + 0.5 * Math.Sin(2.0 * Math.PI * 4800 * dt)) * env;
            }

            // Upward confirmation arpeggio (C6=1046Hz -> E6=1318Hz -> G6=1568Hz)
            if (t >= 0.12)
            {
                var dt = t - 0.12;
                var freq = dt < 0.12 ? 1046.5 : dt < 0.24 ? 1318.5 : 1567.98;
                var env = Math.Exp(-5.0 * dt);
                sample += 0.4 * Math.Sin(2.0 * Math.PI * freq * dt) * env;
            }

            samples[i] = (short)Math.Clamp(sample * short.MaxValue * 0.85, short.MinValue, short.MaxValue);
        }

        return CreateWavPcm16(samples, sampleRate);
    }

    // 3. Top-Up & Level-Up Fanfare: 4-note ascending fanfare with sparkle
    private static byte[] GenerateTopupSound()
    {
        const int sampleRate = 44100;
        const double duration = 1.1;
        var samples = new short[(int)(sampleRate * duration)];
        double[] notes = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
        const double noteDuration = 0.16;

        for (int i = 0; i < samples.Length; i++)
        {
            var t = (double)i / sampleRate;
            int noteIndex = Math.Min(notes.Length - 1, (int)(t / noteDuration));
            double noteTime = t - (noteIndex * noteDuration);
            double freq = notes[noteIndex];

            double env = noteIndex == notes.Length - 1
                ? Math.Exp(-2.8 * noteTime)
                : Math.Exp(-8.0 * noteTime);

            // Rich tone with harmonics
            double sample = (Math.Sin(2.0 * Math.PI * freq * t)
                           + 0.35 * Math.Sin(4.0 * Math.PI * freq * t)
                           + 0.15 * Math.Sin(6.0 * Math.PI * freq * t)) * env * 0.5;

            // Sparkle overtone on final victory note
            if (noteIndex == notes.Length - 1)
            {
                sample += 0.25 * Math.Sin(2.0 * Math.PI * 2093.0 * t) * Math.Exp(-3.5 * noteTime);
            }

            samples[i] = (short)Math.Clamp(sample * short.MaxValue * 0.85, short.MinValue, short.MaxValue);
        }

        return CreateWavPcm16(samples, sampleRate);
    }

    // 4. Subtle high-tech UI click
    private static byte[] GenerateClickSound()
    {
        const int sampleRate = 44100;
        const double duration = 0.06;
        var samples = new short[(int)(sampleRate * duration)];

        for (int i = 0; i < samples.Length; i++)
        {
            var t = (double)i / sampleRate;
            var freq = 1200.0 - 500.0 * (t / duration);
            var env = Math.Exp(-60.0 * t);
            var sample = Math.Sin(2.0 * Math.PI * freq * t) * env * 0.6;
            samples[i] = (short)Math.Clamp(sample * short.MaxValue * 0.85, short.MinValue, short.MaxValue);
        }

        return CreateWavPcm16(samples, sampleRate);
    }

    private static byte[] CreateWavPcm16(short[] samples, int sampleRate)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        int subchunk2Size = samples.Length * sizeof(short);
        int chunkSize = 36 + subchunk2Size;

        // RIFF chunk descriptor
        writer.Write("RIFF"u8.ToArray());
        writer.Write(chunkSize);
        writer.Write("WAVE"u8.ToArray());

        // "fmt " sub-chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Subchunk1Size for PCM
        writer.Write((short)1); // AudioFormat 1 = PCM
        writer.Write((short)1); // NumChannels = 1 (Mono)
        writer.Write(sampleRate); // SampleRate
        writer.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        writer.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
        writer.Write((short)16); // BitsPerSample = 16

        // "data" sub-chunk
        writer.Write("data"u8.ToArray());
        writer.Write(subchunk2Size);
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }
}
