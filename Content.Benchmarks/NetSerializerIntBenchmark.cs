using System;
using System.Buffers.Binary;
using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Content.Benchmarks
{
    [SimpleJob]
    [Virtual]
    public class NetSerializerIntBenchmark
    {
        private MemoryStream _writeStream;
        private MemoryStream _readStream;
        private readonly ushort _x16 = 5;
        private readonly uint _x32 = 5;
        private readonly ulong _x64 = 5;
        private ushort _read16;
        private uint _read32;
        private ulong _read64;

        [GlobalSetup]
        public void Setup()
        {
            _writeStream = new MemoryStream(64);
            _readStream = new MemoryStream();
            _readStream.Write(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8 });
        }

        [Benchmark]
        public void BenchWrite16Span()
        {
            _writeStream.Position = 0;
            WriteUInt16Span(_writeStream, _x16);
        }

        [Benchmark]
        public void BenchWrite32Span()
        {
            _writeStream.Position = 0;
            WriteUInt32Span(_writeStream, _x32);
        }

        [Benchmark]
        public void BenchWrite64Span()
        {
            _writeStream.Position = 0;
            WriteUInt64Span(_writeStream, _x64);
        }

        [Benchmark]
        public void BenchRead16Span()
        {
            _readStream.Position = 0;
            _read16 = ReadUInt16Span(_readStream);
        }

        [Benchmark]
        public void BenchRead32Span()
        {
            _readStream.Position = 0;
            _read32 = ReadUInt32Span(_readStream);
        }

        [Benchmark]
        public void BenchRead64Span()
        {
            _readStream.Position = 0;
            _read64 = ReadUInt64Span(_readStream);
        }

        [Benchmark]
        public void BenchWrite16Byte()
        {
            _writeStream.Position = 0;
            WriteUInt16Byte(_writeStream, _x16);
        }

        [Benchmark]
        public void BenchWrite32Byte()
        {
            _writeStream.Position = 0;
            WriteUInt32Byte(_writeStream, _x32);
        }

        [Benchmark]
        public void BenchWrite64Byte()
        {
            _writeStream.Position = 0;
            WriteUInt64Byte(_writeStream, _x64);
        }

        [Benchmark]
        public void BenchRead16Byte()
        {
            _readStream.Position = 0;
            _read16 = ReadUInt16Byte(_readStream);
        }
        [Benchmark]
        public void BenchRead32Byte()
        {
            _readStream.Position = 0;
            _read32 = ReadUInt32Byte(_readStream);
        }

        [Benchmark]
        public void BenchRead64Byte()
        {
            _readStream.Position = 0;
            _read64 = ReadUInt64Byte(_readStream);
        }

        private static void WriteUInt16Byte(Stream stream, ushort value)
        {
            stream.WriteByte((byte) value);
            stream.WriteByte((byte) (value >> 8));
        }

        private static void WriteUInt32Byte(Stream stream, uint value)
        {
            stream.WriteByte((byte) value);
            stream.WriteByte((byte) (value >> 8));
            stream.WriteByte((byte) (value >> 16));
            stream.WriteByte((byte) (value >> 24));
        }

        private static void WriteUInt64Byte(Stream stream, ulong value)
        {
            stream.WriteByte((byte) value);
            stream.WriteByte((byte) (value >> 8));
            stream.WriteByte((byte) (value >> 16));
            stream.WriteByte((byte) (value >> 24));
            stream.WriteByte((byte) (value >> 32));
            stream.WriteByte((byte) (value >> 40));
            stream.WriteByte((byte) (value >> 48));
            stream.WriteByte((byte) (value >> 56));
        }

        private static ushort ReadUInt16Byte(Stream stream)
        {
            ushort a = 0;

            for (var i = 0; i < 16; i += 8)
            {
                var val = stream.ReadByte();
                if (val == -1)
                    throw new EndOfStreamException();

                a |= (ushort) (val << i);
            }

            return a;
        }

        private static uint ReadUInt32Byte(Stream stream)
        {
            uint a = 0;

            for (var i = 0; i < 32; i += 8)
            {
                var val = stream.ReadByte();
                if (val == -1)
                    throw new EndOfStreamException();

                a |= (uint) val << i;
            }

            return a;
        }

        private static ulong ReadUInt64Byte(Stream stream)
        {
            ulong a = 0;

            for (var i = 0; i < 64; i += 8)
            {
                var val = stream.ReadByte();
                if (val == -1)
                    throw new EndOfStreamException();

                a |= (ulong) val << i;
            }

            return a;
        }

        private static void WriteUInt16Span(Stream stream, ushort value)
        {
            Span<byte> buf = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, value);

            stream.Write(buf);
        }

        private static void WriteUInt32Span(Stream stream, uint value)
        {
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buf, value);

            stream.Write(buf);
        }

        private static void WriteUInt64Span(Stream stream, ulong value)
        {
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, value);

            stream.Write(buf);
        }

        private static ushort ReadUInt16Span(Stream stream)
        {
            Span<byte> buf = stackalloc byte[2];
            var wSpan = buf;

            while (true)
            {
                var read = stream.Read(wSpan);
                if (read == 0)
                    throw new EndOfStreamException();
                if (read == wSpan.Length)
                    break;
                wSpan = wSpan[read..];
            }

            return BinaryPrimitives.ReadUInt16LittleEndian(buf);
        }

        private static uint ReadUInt32Span(Stream stream)
        {
            Span<byte> buf = stackalloc byte[4];
            var wSpan = buf;

            while (true)
            {
                var read = stream.Read(wSpan);
                if (read == 0)
                    throw new EndOfStreamException();
                if (read == wSpan.Length)
                    break;
                wSpan = wSpan[read..];
            }

            return BinaryPrimitives.ReadUInt32LittleEndian(buf);
        }

        private static ulong ReadUInt64Span(Stream stream)
        {
            Span<byte> buf = stackalloc byte[8];
            var wSpan = buf;

            while (true)
            {
                var read = stream.Read(wSpan);
                if (read == 0)
                    throw new EndOfStreamException();
                if (read == wSpan.Length)
                    break;
                wSpan = wSpan[read..];
            }

            return BinaryPrimitives.ReadUInt64LittleEndian(buf);
        }
    }
}
