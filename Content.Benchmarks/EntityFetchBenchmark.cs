using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Utility;

namespace Content.Benchmarks
{
    [SimpleJob]
    [Virtual]
    public class EntityFetchBenchmark
    {
        [Params(1000)] public int N { get; set; }

        public int M { get; set; } = 10;

        private readonly DictEntityStorage _dictStorage = new();
        private readonly GenEntityStorage _genStorage = new();

        private IEntityStorage<DictEntity, DictEntityUid> _dictStorageInterface;
        private IEntityStorage<GenEntity, GenEntityUid> _genStorageInterface;

        private DictEntityUid[] _toReadDict;
        private DictEntity[] _toWriteDict;

        private GenEntityUid[] _toReadGen;
        private GenEntity[] _toWriteGen;

        [GlobalSetup]
        public void Setup()
        {
            _dictStorageInterface = _dictStorage;
            _genStorageInterface = _genStorage;

            var r = new Random();

            var allocatedGen = new List<GenEntity>();
            var allocatedDict = new List<DictEntity>();

            for (var i = 0; i < N; i++)
            {
                allocatedGen.Add(_genStorage.NewEntity());
                allocatedDict.Add(_dictStorage.NewEntity());
            }

            var delTo = N / 2;
            for (var i = 0; i < delTo; i++)
            {
                var index = r.Next(allocatedDict.Count);

                var gEnt = allocatedGen[index];
                var dEnt = allocatedDict[index];

                _genStorage.DeleteEntity(gEnt);
                _dictStorage.DeleteEntity(dEnt);

                allocatedGen.RemoveSwap(i);
                allocatedDict.RemoveSwap(i);
            }

            for (var i = 0; i < N; i++)
            {
                allocatedGen.Add(_genStorage.NewEntity());
                allocatedDict.Add(_dictStorage.NewEntity());
            }

            for (var i = 0; i < delTo; i++)
            {
                var index = r.Next(allocatedDict.Count);

                var gEnt = allocatedGen[index];
                var dEnt = allocatedDict[index];

                _genStorage.DeleteEntity(gEnt);
                _dictStorage.DeleteEntity(dEnt);

                allocatedGen.RemoveSwap(i);
                allocatedDict.RemoveSwap(i);
            }

            _toReadDict = new DictEntityUid[M];
            _toWriteDict = new DictEntity[M];
            _toReadGen = new GenEntityUid[M];
            _toWriteGen = new GenEntity[M];

            for (var i = 0; i < M; i++)
            {
                var index = r.Next(allocatedDict.Count);

                _toReadDict[i] = allocatedDict[index].Uid;
                _toReadGen[i] = allocatedGen[index].Uid;
            }
        }

        [Benchmark]
        public void BenchGenId()
        {
            for (var i = 0; i < M; i++)
            {
                var uid = _toReadGen[i];
                if (_genStorage.TryGetEntity(uid, out var entity))
                {
                    _toWriteGen[i] = entity;
                }
            }
        }

        [Benchmark]
        public void BenchDict()
        {
            for (var i = 0; i < M; i++)
            {
                var uid = _toReadDict[i];
                if (_dictStorage.TryGetEntity(uid, out var entity))
                {
                    _toWriteDict[i] = entity;
                }
            }
        }

        [Benchmark]
        public void BenchGenIdInterface()
        {
            for (var i = 0; i < M; i++)
            {
                var uid = _toReadGen[i];
                if (_genStorageInterface.TryGetEntity(uid, out var entity))
                {
                    _toWriteGen[i] = entity;
                }
            }
        }

        [Benchmark]
        public void BenchDictInterface()
        {
            for (var i = 0; i < M; i++)
            {
                var uid = _toReadDict[i];
                if (_dictStorageInterface.TryGetEntity(uid, out var entity))
                {
                    _toWriteDict[i] = entity;
                }
            }
        }

        private sealed class DictEntityStorage : EntityStorage<DictEntity, DictEntityUid>
        {
            private int _nextValue;

            private readonly Dictionary<DictEntityUid, DictEntity> _dict = new();

            public override bool TryGetEntity(DictEntityUid entityUid, out DictEntity entity)
            {
                if (!_dict.TryGetValue(entityUid, out entity))
                {
                    return false;
                }

                return !entity.Deleted;
            }

            public DictEntity NewEntity()
            {
                var e = new DictEntity(new DictEntityUid(_nextValue++));
                _dict.Add(e.Uid, e);
                return e;
            }

            public void DeleteEntity(DictEntity e)
            {
                DebugTools.Assert(!e.Deleted);

                e.Deleted = true;

                _dict.Remove(e.Uid);
            }
        }

        private interface IEntityStorage<TEntity, TEntityUid>
        {
            public bool TryGetEntity(TEntityUid entityUid, out TEntity entity);
        }

        private abstract class EntityStorage<TEntity, TEntityUid> : IEntityStorage<TEntity, TEntityUid>
        {
            public abstract bool TryGetEntity(TEntityUid entityUid, out TEntity entity);

            public TEntity GetEntity(TEntityUid entityUid)
            {
                if (!TryGetEntity(entityUid, out var entity))
                {
                    throw new ArgumentException();
                }

                return entity;
            }
        }

        private sealed class GenEntityStorage : EntityStorage<GenEntity, GenEntityUid>
        {
            private (int generation, GenEntity entity)[] _entities = new (int, GenEntity)[1];
            private readonly List<int> _availableSlots = new() {0};

            public override bool TryGetEntity(GenEntityUid entityUid, out GenEntity entity)
            {
                var (generation, genEntity) = _entities[entityUid.Index];
                entity = genEntity;

                return generation == entityUid.Generation;
            }

            public GenEntity NewEntity()
            {
                if (_availableSlots.Count == 0)
                {
                    // Reallocate
                    var oldEntities = _entities;
                    _entities = new (int, GenEntity)[_entities.Length * 2];
                    oldEntities.CopyTo(_entities, 0);

                    for (var i = oldEntities.Length; i < _entities.Length; i++)
                    {
                        _availableSlots.Add(i);
                    }
                }

                var index = _availableSlots.Pop();
                ref var slot = ref _entities[index];
                var slotEntity = new GenEntity(new GenEntityUid(slot.generation, index));
                slot.entity = slotEntity;

                return slotEntity;
            }

            public void DeleteEntity(GenEntity e)
            {
                DebugTools.Assert(!e.Deleted);

                e.Deleted = true;

                ref var slot = ref _entities[e.Uid.Index];
                slot.entity = null;
                slot.generation += 1;

                _availableSlots.Add(e.Uid.Index);
            }
        }


        private readonly struct DictEntityUid : IEquatable<DictEntityUid>
        {
            public readonly int Value;

            public DictEntityUid(int value)
            {
                Value = value;
            }

            public bool Equals(DictEntityUid other)
            {
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is DictEntityUid other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Value;
            }

            public static bool operator ==(DictEntityUid left, DictEntityUid right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(DictEntityUid left, DictEntityUid right)
            {
                return !left.Equals(right);
            }
        }

        private readonly struct GenEntityUid
        {
            public readonly int Generation;
            public readonly int Index;

            public GenEntityUid(int generation, int index)
            {
                Generation = generation;
                Index = index;
            }
        }

        private sealed class DictEntity
        {
            public DictEntity(DictEntityUid uid)
            {
                Uid = uid;
            }

            public DictEntityUid Uid { get; }

            public bool Deleted { get; set; }
        }

        private sealed class GenEntity
        {
            public GenEntityUid Uid { get; }

            public bool Deleted { get; set; }

            public GenEntity(GenEntityUid uid)
            {
                Uid = uid;
            }
        }
    }
}
