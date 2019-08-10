using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Maths;
using SysVector4 = System.Numerics.Vector4;

namespace Content.Benchmarks
{
    public class ColorInterpolateBenchmark
    {
        private readonly List<(Color, Color)> _colors = new List<(Color, Color)>();

        [Params(100)] public int N { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(3005);

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

                _colors.Add((new Color(r1, g1, b1, a1), new Color(r2, g2, b2, a2)));
            }
        }

        [Benchmark]
        public void BenchSimple()
        {
            foreach (var (a, b) in _colors)
            {
                InterpolateSimple(a, b, 0.5f);
            }
        }

        //[Benchmark]
        public void BenchSysVector4()
        {
            foreach (var (a, b) in _colors)
            {
                InterpolateSysVector4(a, b, 0.5f);
            }
        }

        //[Benchmark]
        public void BenchSysVector4Blit()
        {
            foreach (var (a, b) in _colors)
            {
                InterpolateSysVector4Blit(a, b, 0.5f);
            }
        }

        //[Benchmark]
        public void BenchSysVector4BlitNoException()
        {
            foreach (var (a, b) in _colors)
            {
                InterpolateSysVector4BlitNoException(a, b, 0.5f);
            }
        }

        [Benchmark]
        public void BenchSysVector4AsRefNoException()
        {
            foreach (var (a, b) in _colors)
            {
                InterpolateSysVector4BlitNoExceptionAsRef(a, b, 0.5f);
            }
        }

        public static Color InterpolateSimple(Color endPoint1, Color endPoint2, float lambda)
        {
            if (lambda < 0 || lambda > 1)
                throw new ArgumentOutOfRangeException(nameof(lambda));
            return new Color(
                endPoint1.R * lambda + endPoint2.R * (1 - lambda),
                endPoint1.G * lambda + endPoint2.G * (1 - lambda),
                endPoint1.B * lambda + endPoint2.B * (1 - lambda),
                endPoint1.A * lambda + endPoint2.A * (1 - lambda)
            );
        }

        public static Color InterpolateSysVector4(Color endPoint1, Color endPoint2, float lambda)
        {
            if (lambda < 0 || lambda > 1)
                throw new ArgumentOutOfRangeException(nameof(lambda));

            var vec1 = new SysVector4(endPoint1.R, endPoint1.G, endPoint1.B, endPoint1.A);
            var vec2 = new SysVector4(endPoint2.R, endPoint2.G, endPoint2.B, endPoint2.A);

            var res = SysVector4.Lerp(vec1, vec2, 1 - lambda);

            return new Color(
                res.X, res.Y, res.Z, res.W);
        }

        public static unsafe Color InterpolateSysVector4Blit(in Color endPoint1, in Color endPoint2, float lambda)
        {
            if (lambda < 0 || lambda > 1)
                throw new ArgumentOutOfRangeException(nameof(lambda));


            fixed (Color* p1 = &endPoint1)
            fixed (Color* p2 = &endPoint2)
            {
                var vp1 = (SysVector4*) p1;
                var vp2 = (SysVector4*) p2;

                var res = SysVector4.Lerp(*vp1, *vp2, 1 - lambda);

                return *(Color*) (&res);
            }
        }

        public static unsafe Color InterpolateSysVector4BlitNoException(in Color endPoint1, in Color endPoint2,
            float lambda)
        {
            fixed (Color* p1 = &endPoint1)
            fixed (Color* p2 = &endPoint2)
            {
                var vp1 = (SysVector4*) p1;
                var vp2 = (SysVector4*) p2;

                var res = SysVector4.Lerp(*vp2, *vp1, lambda);

                return *(Color*) (&res);
            }
        }

        public static unsafe Color InterpolateSysVector4BlitNoExceptionAsRef(in Color endPoint1, in Color endPoint2,
            float lambda)
        {
            ref var sv1 = ref Unsafe.As<Color, SysVector4>(ref Unsafe.AsRef(endPoint1));
            ref var sv2 = ref Unsafe.As<Color, SysVector4>(ref Unsafe.AsRef(endPoint2));

            var res = SysVector4.Lerp(sv2, sv1, lambda);

            return Unsafe.As<SysVector4, Color>(ref res);
        }
    }
}
