using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Content.Benchmarks
{
    [Virtual]
    public class StereoToMonoBenchmark
    {
        [Params(128, 256, 512)]
        public int N { get; set; }

        private short[] _input;
        private short[] _output;

        [GlobalSetup]
        public void Setup()
        {
            _input = new short[N * 2];
            _output = new short[N];
        }

        [Benchmark]
        public void BenchSimple()
        {
            var l = N;
            for (var j = 0; j < l; j++)
            {
                var k = j + l;
                _output[j] = (short) ((_input[k] + _input[j]) / 2);
            }
        }

        [Benchmark]
        public unsafe void BenchSse()
        {
            var l = N;
            fixed (short* iPtr = _input)
            fixed (short* oPtr = _output)
            {
                for (var j = 0; j < l; j += 8)
                {
                    var k = j + l;

                    var jV = Sse2.ShiftRightArithmetic(Sse2.LoadVector128(iPtr + j), 1);
                    var kV = Sse2.ShiftRightArithmetic(Sse2.LoadVector128(iPtr + k), 1);

                    Sse2.Store(j + oPtr, Sse2.Add(jV, kV));
                }
            }
        }

        [Benchmark]
        public unsafe void BenchAvx2()
        {
            var l = N;
            fixed (short* iPtr = _input)
            fixed (short* oPtr = _output)
            {
                for (var j = 0; j < l; j += 16)
                {
                    var k = j + l;

                    var jV = Avx2.ShiftRightArithmetic(Avx.LoadVector256(iPtr + j), 1);
                    var kV = Avx2.ShiftRightArithmetic(Avx.LoadVector256(iPtr + k), 1);

                    Avx.Store(j + oPtr, Avx2.Add(jV, kV));
                }
            }
        }
    }
}
