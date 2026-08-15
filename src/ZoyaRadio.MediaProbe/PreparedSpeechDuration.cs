using Windows.Storage.Streams;

namespace ZoyaRadio.MediaProbe;

internal static class PreparedSpeechDuration
{
    private const uint HeaderReadLimit = 4096;

    internal static async Task<TimeSpan> ReadWaveDurationAsync(
        IRandomAccessStream stream,
        CancellationToken cancellationToken)
    {
        if (stream.Size < 44)
        {
            throw new InvalidOperationException("Prepared speech did not contain a readable WAV header.");
        }

        uint requested = (uint)Math.Min((ulong)HeaderReadLimit, stream.Size);
        using IInputStream input = stream.GetInputStreamAt(0);
        using var reader = new DataReader(input);
        uint loaded = await reader.LoadAsync(requested).AsTask(cancellationToken);
        var bytes = new byte[loaded];
        reader.ReadBytes(bytes);

        if (bytes.Length < 12
            || !HasChunk(bytes, 0, "RIFF")
            || !HasChunk(bytes, 8, "WAVE"))
        {
            throw new InvalidOperationException("Prepared speech did not contain a valid WAV header.");
        }

        uint? byteRate = null;
        uint? dataLength = null;
        int offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            uint chunkLength = ReadUInt32LittleEndian(bytes, offset + 4);
            int chunkData = offset + 8;

            if (chunkId == "fmt " && chunkLength >= 16 && chunkData + 16 <= bytes.Length)
            {
                byteRate = ReadUInt32LittleEndian(bytes, chunkData + 8);
            }
            else if (chunkId == "data")
            {
                dataLength = chunkLength;
            }

            if (byteRate is not null && dataLength is not null)
            {
                break;
            }

            long next = chunkData + (long)chunkLength + (chunkLength % 2);
            if (next > bytes.Length || next > int.MaxValue)
            {
                break;
            }

            offset = (int)next;
        }

        if (byteRate is null || byteRate == 0 || dataLength is null || dataLength == 0)
        {
            throw new InvalidOperationException("Prepared speech WAV duration could not be measured safely.");
        }

        TimeSpan duration = TimeSpan.FromSeconds(dataLength.Value / (double)byteRate.Value);
        if (duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Prepared speech WAV duration was not positive.");
        }

        return duration;
    }

    private static bool HasChunk(IReadOnlyList<byte> bytes, int offset, string text)
        => offset + text.Length <= bytes.Count
            && text.Select((character, index) => bytes[offset + index] == character).All(value => value);

    private static uint ReadUInt32LittleEndian(IReadOnlyList<byte> bytes, int offset)
        => (uint)(bytes[offset]
            | (bytes[offset + 1] << 8)
            | (bytes[offset + 2] << 16)
            | (bytes[offset + 3] << 24));
}