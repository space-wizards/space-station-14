#if NETCOREAPP
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using SysVector4 = System.Numerics.Vector4;

namespace Content.Benchmarks
{
    [DisassemblyDiagnoser]
    [Virtual]
    public class ColorInterpolateBenchmark
    {
#if NETCOREAPP
        private const MethodImplOptions AggressiveOpt = MethodImplOptions.AggressiveOptimization;
#else
        private const MethodImplOptions AggressiveOpt = default;
#endif

        private (Color, Color)[] _colors;
        private Color[] _output;

        [Params(100)] public int N { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(3005);

            _colors = new (Color, Color)[N];
            _output = new Color[N];

            for (var i = 0; i < N; i++)
            {
                var r1 = random.NextFloat();
                var g1 = random.NextFloat();
                var b1 = random.NextFloat();
                var a1 = random.NextFloat();

                var r2 = random.NextFloat();
                var g2 = random.NextFloat();
                var b2 = random.NextFloat();
                var a2 = random.NextFloat();

                _colors[i] = (new Color(r1, g1, b1, a1), new Color(r2, g2, b2, a2));
            }
        }

        [Benchmark]
        public void BenchSimple()
        {
            for (var i = 0; i < N; i++)
            {
                ref var tuple = ref _colors[i];
                _output[i] = InterpolateSimple(tuple.Item1, tuple.Item2, 0.5f);
            }
        }


        [Benchmark]
        public void BenchSysVector4In()
        {
            for (var i = 0; i < N; i++)
            {
                ref var tuple = ref _colors[i];
                _output[i] = InterpolateSysVector4In(tuple.Item1, tuple.Item2, 0.5f);
            }
        }

        [Benchmark]
        public void BenchSysVector4()
        {
            for (var i = 0; i < N; i++)
            {
                ref var tuple = ref _colors[i];
                _output[i] = InterpolateSysVector4(tuple.Item1, tuple.Item2, 0.5f);
            }
        }

#if NETCOREAPP
        [Benchmark]
        public void BenchSimd()
        {
            for (var i = 0; i < N; i++)
            {
                ref var tuple = ref _colors[i];
                _output[i] = InterpolateSimd(tuple.Item1, tuple.Item2, 0.5f);
            }
        }

        [Benchmark]
        public void BenchSimdIn()
        {
            for (var i = 0; i < N; i++)
            {
                ref var tuple = ref _colors[i];
                _output[i] = InterpolateSimdIn(tuple.Item1, tuple.Item2, 0.5f);
            }
        }
#endif

        [MethodImpl(AggressiveOpt)]
        public static Color InterpolateSimple(Color a, Color b, float lambda)
        {
            return new(
                a.R + (b.R - a.R) * lambda,
                a.G + (b.G - a.G) * lambda,
                a.B + (b.G - a.B) * lambda,
                a.A + (b.A - a.A) * lambda
            );
        }

        [MethodImpl(AggressiveOpt)]
        public static Color InterpolateSysVector4(Color a, Color b,
            float lambda)
        {
            ref var sva = ref Unsafe.As<Color, SysVector4>(ref a);
            ref var svb = ref Unsafe.As<Color, SysVector4>(ref b);

            var res = SysVector4.Lerp(sva, svb, lambda);

            return Unsafe.As<SysVector4, Color>(ref res);
        }

        [MethodImpl(AggressiveOpt)]
        public static Color InterpolateSysVector4In(in Color endPoint1, in Color endPoint2,
            float lambda)
        {
            ref var sva = ref Unsafe.As<Color, SysVector4>(ref Unsafe.AsRef(in endPoint1));
            ref var svb = ref Unsafe.As<Color, SysVector4>(ref Unsafe.AsRef(in endPoint2));

            var res = SysVector4.Lerp(svb, sva, lambda);

            return Unsafe.As<SysVector4, Color>(ref res);
        }

#if NETCOREAPP
        [MethodImpl(AggressiveOpt)]
        public static Color InterpolateSimd(Color a, Color b,
            float lambda)
        {
            var vecA = Unsafe.As<Color, Vector128<float>>(ref a);
            var vecB = Unsafe.As<Color, Vector128<float>>(ref b);

            vecB = Fma.MultiplyAdd(Sse.Subtract(vecB, vecA), Vector128.Create(lambda), vecA);

            return Unsafe.As<Vector128<float>, Color>(ref vecB);
        }

        [MethodImpl(AggressiveOpt)]
        public static Color InterpolateSimdIn(in Color a, in Color b,
            float lambda)
        {
            var vecA = Unsafe.As<Color, Vector128<float>>(ref Unsafe.AsRef(in a));
            var vecB = Unsafe.As<Color, Vector128<float>>(ref Unsafe.AsRef(in b));

            vecB = Fma.MultiplyAdd(Sse.Subtract(vecB, vecA), Vector128.Create(lambda), vecA);

            return Unsafe.As<Vector128<float>, Color>(ref vecB);
        }
#endif
    }
}
