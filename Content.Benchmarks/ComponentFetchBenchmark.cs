using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Utility;

namespace Content.Benchmarks
{
    [SimpleJob]
    [Virtual]
    public class ComponentFetchBenchmark
    {
        [Params(5000)] public int NEnt { get; set; }

        private readonly Dictionary<(EntityUid, Type), BComponent>
            _componentsFlat = new();

        private readonly Dictionary<Type, Dictionary<EntityUid, BComponent>> _componentsPart =
            new();

        private UniqueIndex<Type, BComponent> _allComponents = new();

        private readonly List<EntityUid> _lookupEntities = new();

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random();

            _componentsPart[typeof(BComponent1)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent2)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent3)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent4)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponentLookup)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent6)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent7)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent8)] = new Dictionary<EntityUid, BComponent>();
            _componentsPart[typeof(BComponent9)] = new Dictionary<EntityUid, BComponent>();

            for (var i = 0u; i < NEnt; i++)
            {
                var eId = new EntityUid(i);

                if (random.Next(1) == 0)
                {
                    _lookupEntities.Add(eId);
                }

                var comps = new List<BComponent>
                {
                    new BComponent1(),
                    new BComponent2(),
                    new BComponent3(),
                    new BComponent4(),
                    new BComponent6(),
                    new BComponent7(),
                    new BComponent8(),
                    new BComponent9(),
                };

                if (random.Next(1000) == 0)
                {
                    comps.Add(new BComponentLookup());
                }

                foreach (var comp in comps)
                {
                    comp.Uid = eId;
                    var type = comp.GetType();
                    _componentsPart[type][eId] = comp;
                    _componentsFlat[(eId, type)] = comp;
                    _allComponents.Add(type, comp);
                }
            }
        }

        // These two benchmarks are find "needles in haystack" components.
        // We try to look up a component that 0.1% of entities have on 1% of entities.
        // Examples of this in the engine are VisibilityComponent lookups during PVS.
        [Benchmark]
        public void FindPart()
        {
            foreach (var entityUid in _lookupEntities)
            {
                var d = _componentsPart[typeof(BComponentLookup)];
                d.TryGetValue(entityUid, out _);
            }
        }

        [Benchmark]
        public void FindFlat()
        {
            foreach (var entityUid in _lookupEntities)
            {
                _componentsFlat.TryGetValue((entityUid, typeof(BComponentLookup)), out _);
            }
        }

        // Iteration benchmarks:
        // We try to iterate every instance of a single component (BComponent1) and see which is faster.
        [Benchmark]
        public void IterPart()
        {
            var list = _componentsPart[typeof(BComponent1)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list.Values)
            {
                arr[i++] = c;
            }
        }

        [Benchmark]
        public void IterFlat()
        {
            var list = _allComponents[typeof(BComponent1)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list)
            {
                arr[i++] = c;
            }
        }

        // We do the same as the iteration benchmarks but re-fetch the component every iteration.
        // This is what entity systems mostly do via entity queries because crappy code.
        [Benchmark]
        public void IterFetchPart()
        {
            var list = _componentsPart[typeof(BComponent1)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list.Values)
            {
                var eId = c.Uid;
                var d = _componentsPart[typeof(BComponent1)];
                arr[i++] = d[eId];
            }
        }

        [Benchmark]
        public void IterFetchFlat()
        {
            var list = _allComponents[typeof(BComponent1)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list)
            {
                var eId = c.Uid;
                arr[i++] = _componentsFlat[(eId, typeof(BComponent1))];
            }
        }

        // Same as the previous benchmarks but with BComponentLookup instead.
        // Which is only on 1% of entities.
        [Benchmark]
        public void IterFetchPartRare()
        {
            var list = _componentsPart[typeof(BComponentLookup)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list.Values)
            {
                var eId = c.Uid;
                var d = _componentsPart[typeof(BComponentLookup)];
                arr[i++] = d[eId];
            }
        }

        [Benchmark]
        public void IterFetchFlatRare()
        {
            var list = _allComponents[typeof(BComponentLookup)];
            var arr = new BComponent[list.Count];
            var i = 0;
            foreach (var c in list)
            {
                var eId = c.Uid;
                arr[i++] = _componentsFlat[(eId, typeof(BComponentLookup))];
            }
        }

        private readonly struct EntityUid : IEquatable<EntityUid>
        {
            public readonly uint Value;

            public EntityUid(uint value)
            {
                Value = value;
            }

            public bool Equals(EntityUid other)
            {
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is EntityUid other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (int) Value;
            }

            public static bool operator ==(EntityUid left, EntityUid right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(EntityUid left, EntityUid right)
            {
                return !left.Equals(right);
            }
        }

        private abstract class BComponent
        {
            public EntityUid Uid;
        }

        private sealed class BComponent1 : BComponent
        {
        }

        private sealed class BComponent2 : BComponent
        {
        }

        private sealed class BComponent3 : BComponent
        {
        }

        private sealed class BComponent4 : BComponent
        {
        }

        private sealed class BComponentLookup : BComponent
        {
        }

        private sealed class BComponent6 : BComponent
        {
        }

        private sealed class BComponent7 : BComponent
        {
        }

        private sealed class BComponent8 : BComponent
        {
        }

        private sealed class BComponent9 : BComponent
        {
        }
    }
}
