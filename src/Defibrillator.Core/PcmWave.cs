using System;
using System.IO;
using System.Text;

namespace Defibrillator.Core;

/// <summary>Strict decoder for the bundled mono PCM16 animal clips, limited to three seconds.</summary>
public sealed class PcmWave
{
    public int SampleRate { get; }
    public float[] Samples { get; }
    public double Seconds => Samples.Length / (double)SampleRate;
    private PcmWave(int rate, float[] samples) { SampleRate = rate; Samples = samples; }

    public static PcmWave Read(Stream input)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            memory.Write(buffer, 0, read);
            if (memory.Length > 400000) throw new InvalidDataException("Animal WAV exceeds size limit.");
        }
        memory.Position = 0;
        using var reader = new BinaryReader(memory, Encoding.ASCII, true);
        string Four() => Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (memory.Length < 44 || Four() != "RIFF") throw new InvalidDataException("Expected RIFF WAV.");
        uint riffSize = reader.ReadUInt32();
        if (riffSize + 8L != memory.Length || Four() != "WAVE") throw new InvalidDataException("Invalid WAV container.");
        int rate = 0; bool format = false; byte[]? pcm = null;
        while (memory.Position + 8 <= memory.Length)
        {
            string chunk = Four(); uint size = reader.ReadUInt32();
            long end = memory.Position + size;
            if (end > memory.Length) throw new InvalidDataException("Truncated WAV chunk.");
            if (chunk == "fmt ")
            {
                if (format || size < 16 || reader.ReadUInt16() != 1 || reader.ReadUInt16() != 1)
                    throw new InvalidDataException("Expected mono PCM.");
                rate = reader.ReadInt32(); int byteRate = reader.ReadInt32();
                int align = reader.ReadUInt16(); int bits = reader.ReadUInt16();
                if ((rate != 44100 && rate != 48000) || bits != 16 || align != 2 || byteRate != rate * 2)
                    throw new InvalidDataException("Expected PCM16 at 44100/48000 Hz.");
                format = true;
            }
            else if (chunk == "data")
            {
                if (pcm != null || size == 0 || size % 2 != 0) throw new InvalidDataException("Invalid PCM data.");
                pcm = reader.ReadBytes((int)size);
            }
            memory.Position = end + (size % 2);
            if (memory.Position > memory.Length) throw new InvalidDataException("Missing WAV padding.");
        }
        if (!format || pcm == null || pcm.Length / 2 > rate * 3) throw new InvalidDataException("Missing audio or clip exceeds three seconds.");
        var samples = new float[pcm.Length / 2];
        for (int i = 0; i < samples.Length; i++) samples[i] = (short)(pcm[2 * i] | pcm[2 * i + 1] << 8) / 32768f;
        return new PcmWave(rate, samples);
    }
}
