/*
using BenchmarkDotNet.Attributes;
using Robust.Shared.IoC;

namespace Content.Benchmarks
{
    // To actually run this benchmark you'll have to make DependencyCollection public so it's accessible.

    [Virtual]
    public class DependencyInjectBenchmark
    {
        [Params(InjectMode.Reflection, InjectMode.DynamicMethod)]
        public InjectMode Mode { get; set; }

        private DependencyCollection _dependencyCollection;

        [GlobalSetup]
        public void Setup()
        {
            _dependencyCollection = new DependencyCollection();
            _dependencyCollection.Register<X1, X1>();
            _dependencyCollection.Register<X2, X2>();
            _dependencyCollection.Register<X3, X3>();
            _dependencyCollection.Register<X4, X4>();
            _dependencyCollection.Register<X5, X5>();

            _dependencyCollection.BuildGraph();

            switch (Mode)
            {
                case InjectMode.Reflection:
                    break;
                case InjectMode.DynamicMethod:
                    // Running this without oneOff will cause DependencyCollection to cache the DynamicMethod injector.
                    // So future injections (even with oneOff) will keep using the DynamicMethod.
                    // AKA, be fast.
                    _dependencyCollection.InjectDependencies(new TestDummy());
                    break;
            }
        }

        [Benchmark]
        public void Inject()
        {
            _dependencyCollection.InjectDependencies(new TestDummy(), true);
        }

        public enum InjectMode
        {
            Reflection,
            DynamicMethod
        }

        private sealed class X1 { }
        private sealed class X2 { }
        private sealed class X3 { }
        private sealed class X4 { }
        private sealed class X5 { }

        private sealed class TestDummy
        {
            [Dependency] private readonly X1 _x1;
            [Dependency] private readonly X2 _x2;
            [Dependency] private readonly X3 _x3;
            [Dependency] private readonly X4 _x4;
            [Dependency] private readonly X5 _x5;
        }
    }
}
*/
