using System.IO;
using System.IO.Compression;
using Content.Shared.Signature;

namespace Content.Server.Signature;

public static class SignatureSerializer
{
    public static readonly int BytePerEntry = 1;

    private const int MaxDimension = 1024;

    private const CompressionLevel Level = CompressionLevel.Optimal;

    public static string Serialize(SignatureData data)
    {
        if (data.Width <= 0 || data.Height <= 0 || data.Width > MaxDimension || data.Height > MaxDimension)
            return string.Empty;

        Span<byte> header = stackalloc byte[8];
        BitConverter.TryWriteBytes(header[..4], data.Width);
        BitConverter.TryWriteBytes(header.Slice(4, 4), data.Height);

        using var memoryStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(memoryStream, Level))
        {
            brotliStream.Write(data.Pixels, 0, data.Pixels.Length);
        }
        var compressedPixels = memoryStream.ToArray();

        var resultBuffer = new byte[8 + compressedPixels.Length];
        header.CopyTo(resultBuffer);
        Buffer.BlockCopy(compressedPixels, 0, resultBuffer, 8, compressedPixels.Length);
        return Convert.ToBase64String(resultBuffer);
    }

    public static SignatureData? Deserialize(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return null;

        try
        {
            var buffer = Convert.FromBase64String(data);
            if (buffer.Length < 8)
                return null;

            var width = BitConverter.ToInt32(buffer, 0);
            var height = BitConverter.ToInt32(buffer, 4);

            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
                return null;

            var expectedRawSize = width * height * BytePerEntry;
            var instance = new SignatureData(width, height);

            if (buffer.Length - 8 == expectedRawSize)
            {
                Buffer.BlockCopy(buffer, 8, instance.Pixels, 0, expectedRawSize);
            }
            else
            {
                using var inputStream = new MemoryStream(buffer, 8, buffer.Length - 8);
                using var decompressStream = new BrotliStream(inputStream, CompressionMode.Decompress);

                decompressStream.ReadExactly(instance.Pixels);
            }

            return instance;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
