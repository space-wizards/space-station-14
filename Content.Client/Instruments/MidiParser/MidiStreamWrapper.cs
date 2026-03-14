using System.IO;
using System.Text;

namespace Content.Client.Instruments.MidiParser;

public sealed class MidiStreamWrapper
{
    private readonly MemoryStream _stream;
    private byte[] _buffer;

    public long StreamPosition => _stream.Position;

    public MidiStreamWrapper(byte[] data)
    {
        _stream = new MemoryStream(data, writable: false);
        _buffer = new byte[4];
    }

    /// <summary>
    /// Skips X number of bytes in the stream.
    /// </summary>
    /// <param name="count">The number of bytes to skip. If 0, no operations on the stream are performed.</param>
    public void Skip(int count)
    {
        if (count == 0)
            return;

        _stream.Seek(count, SeekOrigin.Current);
    }

    public byte ReadByte()
    {
        var b = _stream.ReadByte();
        if (b == -1)
            throw new Exception("Unexpected end of stream");

        return (byte)b;
    }

    /// <summary>
    /// Reads N bytes using the buffer.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        if (_buffer.Length < count)
        {
            Array.Resize(ref _buffer, count);
        }

        var read = _stream.Read(_buffer, 0, count);
        if (read != count)
            throw new Exception("Unexpected end of stream");

        return _buffer;
    }

    /// <summary>
    /// Reads a 4 byte big-endian uint.
    /// </summary>
    public uint ReadUInt32()
    {
        var bytes = ReadBytes(4);
        return (uint)((bytes[0] << 24) |
                      (bytes[1] << 16) |
                      (bytes[2] << 8)  |
                      (bytes[3]));
    }

    /// <summary>
    /// Reads a 2 byte big-endian ushort.
    /// </summary>
    public ushort ReadUInt16()
    {
        var bytes = ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    public string ReadString(int count)
    {
        var bytes = ReadBytes(count);
        return Encoding.UTF8.GetString(bytes, 0, count);
    }

    public uint ReadVariableLengthQuantity()
    {
        uint value = 0;

        // variable-length-quantities encode ints using 7 bits per byte
        // the highest bit (7) is used for a continuation flag. We read until the high bit is 0

        while (true)
        {
            var b = ReadByte();
            value = (value << 7) | (uint)(b & 0x7f); // Shift current value and add 7 bits
            // value << 7, make room for the next 7 bits
            // b & 0x7F mask out the high bit to just get the 7 bit payload
            if ((b & 0x80) == 0)
                break; // This was the last bit.
        }

        return value;
    }
}
