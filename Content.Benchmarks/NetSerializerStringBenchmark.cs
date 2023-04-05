using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Attributes;
using Lidgren.Network;
using NetSerializer;
using Robust.Shared.Analyzers;

namespace Content.Benchmarks
{
    // Code for the *Slow and *Unsafe implementations taken from NetSerializer, licensed under the MIT license.

    [MemoryDiagnoser]
    [Virtual]
    public class NetSerializerStringBenchmark
    {
        private const int StringByteBufferLength = 256;
        private const int StringCharBufferLength = 128;

        private string _toSerialize;

        [Params(8, 64, 256, 1024)]
        public int StringLength { get; set; }

        private readonly MemoryStream _outputStream = new(2048);
        private readonly MemoryStream _inputStream = new(2048);

        [GlobalSetup]
        public void Setup()
        {
            Span<byte> buf = stackalloc byte[StringLength / 2];
            new Random().NextBytes(buf);
            _toSerialize = NetUtility.ToHexString(buf);
            Primitives.WritePrimitive(_inputStream, _toSerialize);
        }

        [Benchmark]
        public void BenchWriteCore()
        {
            _outputStream.Position = 0;
            WritePrimitiveCore(_outputStream, _toSerialize);
        }

        [Benchmark]
        public void BenchReadCore()
        {
            _inputStream.Position = 0;
            ReadPrimitiveCore(_inputStream, out string _);
        }

        [Benchmark]
        public void BenchWriteUnsafe()
        {
            _outputStream.Position = 0;
            WritePrimitiveUnsafe(_outputStream, _toSerialize);
        }

        [Benchmark]
        public void BenchReadUnsafe()
        {
            _inputStream.Position = 0;
            ReadPrimitiveUnsafe(_inputStream, out string _);
        }

        [Benchmark]
        public void BenchWriteSlow()
        {
            _outputStream.Position = 0;
            WritePrimitiveSlow(_outputStream, _toSerialize);
        }

        [Benchmark]
        public void BenchReadSlow()
        {
            _inputStream.Position = 0;
            ReadPrimitiveSlow(_inputStream, out string _);
        }

		public static void WritePrimitiveCore(Stream stream, string value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}

			if (value.Length == 0)
			{
				Primitives.WritePrimitive(stream, (uint)1);
				return;
			}

			Span<byte> buf = stackalloc byte[StringByteBufferLength];

			var totalChars = value.Length;
			var totalBytes = Encoding.UTF8.GetByteCount(value);

			Primitives.WritePrimitive(stream, (uint)totalBytes + 1);
			Primitives.WritePrimitive(stream, (uint)totalChars);

			var totalRead = 0;
			ReadOnlySpan<char> span = value;
			for (;;)
			{
				var finalChunk = totalRead + totalChars >= totalChars;
				Utf8.FromUtf16(span, buf, out var read, out var wrote, isFinalBlock: finalChunk);
				stream.Write(buf.Slice(0, wrote));
				totalRead += read;
				if (read >= totalChars)
				{
					break;
				}

				span = span[read..];
				totalChars -= read;
			}
		}

		private static readonly SpanAction<char, (int, Stream)> _stringSpanRead = StringSpanRead;

		public static void ReadPrimitiveCore(Stream stream, out string value)
		{
			Primitives.ReadPrimitive(stream, out uint totalBytes);

			if (totalBytes == 0)
			{
				value = null;
				return;
			}

			if (totalBytes == 1)
			{
				value = string.Empty;
				return;
			}

			totalBytes -= 1;

            Primitives.ReadPrimitive(stream, out uint totalChars);

			value = string.Create((int) totalChars, ((int) totalBytes, stream), _stringSpanRead);
		}

		private static void StringSpanRead(Span<char> span, (int totalBytes, Stream stream) tuple)
		{
			Span<byte> buf = stackalloc byte[StringByteBufferLength];

			// ReSharper disable VariableHidesOuterVariable
			var (totalBytes, stream) = tuple;
			// ReSharper restore VariableHidesOuterVariable

			var totalBytesRead = 0;
			var totalCharsRead = 0;
			var writeBufStart = 0;

			while (totalBytesRead < totalBytes)
			{
				var bytesLeft = totalBytes - totalBytesRead;
				var bytesReadLeft = Math.Min(buf.Length, bytesLeft);
				var writeSlice = buf.Slice(writeBufStart, bytesReadLeft - writeBufStart);
				var bytesInBuffer = stream.Read(writeSlice);
				if (bytesInBuffer == 0) throw new EndOfStreamException();

				var readFromStream = bytesInBuffer + writeBufStart;
				var final = readFromStream == bytesLeft;
				var status = Utf8.ToUtf16(buf[..readFromStream], span[totalCharsRead..], out var bytesRead, out var charsRead, isFinalBlock: final);

				totalBytesRead += bytesRead;
				totalCharsRead += charsRead;
				writeBufStart = 0;

				if (status == OperationStatus.DestinationTooSmall)
				{
					// Malformed data?
					throw new InvalidDataException();
				}

				if (status == OperationStatus.NeedMoreData)
				{
					// We got cut short in the middle of a multi-byte UTF-8 sequence.
					// So we need to move it to the bottom of the span, then read the next bit *past* that.
					// This copy should be fine because we're only ever gonna be copying up to 4 bytes
					// from the end of the buffer to the start.
					// So no chance of overlap.
					buf[bytesRead..].CopyTo(buf);
					writeBufStart = bytesReadLeft - bytesRead;
					continue;
				}

				Debug.Assert(status == OperationStatus.Done);
			}
		}

		public static void WritePrimitiveSlow(Stream stream, string value)
		{
			if (value == null)
			{
                Primitives.WritePrimitive(stream, (uint)0);
				return;
			}
			else if (value.Length == 0)
			{
                Primitives.WritePrimitive(stream, (uint)1);
				return;
			}

			var encoding = new UTF8Encoding(false, true);

			int len = encoding.GetByteCount(value);

            Primitives.WritePrimitive(stream, (uint)len + 1);
			Primitives.WritePrimitive(stream, (uint)value.Length);

			var buf = new byte[len];

			encoding.GetBytes(value, 0, value.Length, buf, 0);

			stream.Write(buf, 0, len);
		}

		public static void ReadPrimitiveSlow(Stream stream, out string value)
		{
			uint len;
			Primitives.ReadPrimitive(stream, out len);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = string.Empty;
				return;
			}

			uint totalChars;
            Primitives.ReadPrimitive(stream, out totalChars);

			len -= 1;

			var encoding = new UTF8Encoding(false, true);

			var buf = new byte[len];

			int l = 0;

			while (l < len)
			{
				int r = stream.Read(buf, l, (int)len - l);
				if (r == 0)
					throw new EndOfStreamException();
				l += r;
			}

			value = encoding.GetString(buf);
		}

        sealed class StringHelper
		{
			public StringHelper()
			{
				this.Encoding = new UTF8Encoding(false, true);
			}

			Encoder m_encoder;
			Decoder m_decoder;

			byte[] m_byteBuffer;
			char[] m_charBuffer;

			public UTF8Encoding Encoding { get; private set; }
			public Encoder Encoder { get { if (m_encoder == null) m_encoder = this.Encoding.GetEncoder(); return m_encoder; } }
			public Decoder Decoder { get { if (m_decoder == null) m_decoder = this.Encoding.GetDecoder(); return m_decoder; } }

			public byte[] ByteBuffer { get { if (m_byteBuffer == null) m_byteBuffer = new byte[StringByteBufferLength]; return m_byteBuffer; } }
			public char[] CharBuffer { get { if (m_charBuffer == null) m_charBuffer = new char[StringCharBufferLength]; return m_charBuffer; } }
		}

		[ThreadStatic]
		static StringHelper s_stringHelper;

		public unsafe static void WritePrimitiveUnsafe(Stream stream, string value)
		{
			if (value == null)
			{
				Primitives.WritePrimitive(stream, (uint)0);
				return;
			}
			else if (value.Length == 0)
			{
                Primitives.WritePrimitive(stream, (uint)1);
				return;
			}

			var helper = s_stringHelper;
			if (helper == null)
				s_stringHelper = helper = new StringHelper();

			var encoder = helper.Encoder;
			var buf = helper.ByteBuffer;

			int totalChars = value.Length;
			int totalBytes;

			fixed (char* ptr = value)
				totalBytes = encoder.GetByteCount(ptr, totalChars, true);

			Primitives.WritePrimitive(stream, (uint)totalBytes + 1);
			Primitives.WritePrimitive(stream, (uint)totalChars);

			int p = 0;
			bool completed = false;

			while (completed == false)
			{
				int charsConverted;
				int bytesConverted;

				fixed (char* src = value)
				fixed (byte* dst = buf)
				{
					encoder.Convert(src + p, totalChars - p, dst, buf.Length, true,
						out charsConverted, out bytesConverted, out completed);
				}

				stream.Write(buf, 0, bytesConverted);

				p += charsConverted;
			}
		}

		public static void ReadPrimitiveUnsafe(Stream stream, out string value)
		{
			uint totalBytes;
			Primitives.ReadPrimitive(stream, out totalBytes);

			if (totalBytes == 0)
			{
				value = null;
				return;
			}
			else if (totalBytes == 1)
			{
				value = string.Empty;
				return;
			}

			totalBytes -= 1;

			uint totalChars;
			Primitives.ReadPrimitive(stream, out totalChars);

			var helper = s_stringHelper;
			if (helper == null)
				s_stringHelper = helper = new StringHelper();

			var decoder = helper.Decoder;
			var buf = helper.ByteBuffer;
			char[] chars;
			if (totalChars <= StringCharBufferLength)
				chars = helper.CharBuffer;
			else
				chars = new char[totalChars];

			int streamBytesLeft = (int)totalBytes;

			int cp = 0;

			while (streamBytesLeft > 0)
			{
				int bytesInBuffer = stream.Read(buf, 0, Math.Min(buf.Length, streamBytesLeft));
				if (bytesInBuffer == 0)
					throw new EndOfStreamException();

				streamBytesLeft -= bytesInBuffer;
				bool flush = streamBytesLeft == 0;

				bool completed = false;

				int p = 0;

				while (completed == false)
				{
					int charsConverted;
					int bytesConverted;

					decoder.Convert(buf, p, bytesInBuffer - p,
						chars, cp, (int)totalChars - cp,
						flush,
						out bytesConverted, out charsConverted, out completed);

					p += bytesConverted;
					cp += charsConverted;
				}
			}

			value = new string(chars, 0, (int)totalChars);
		}
    }
}
