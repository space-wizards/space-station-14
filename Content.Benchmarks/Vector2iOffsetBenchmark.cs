using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Content.Shared.Atmos;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Benchmark for testing different ways of offsetting <see cref="Vector2i"/>.
/// </summary>
/// <remarks>This is an excuse for me to use Rider's new ASM viewer
/// and pretend that I know what I'm doing.</remarks>
[Virtual]
[GcServer(true)]
public class Vector2iOffsetBenchmark
{
    private Vector2i[] _vecs = default!;
    private AtmosDirection[] _dirs = default!;

    public const int Count = 1000;

    [GlobalSetup]
    public void Setup()
    {
        _vecs = new Vector2i[Count];
        _dirs = new AtmosDirection[Count];
        var rand = new Random();
        for (var i = 0; i < Count; i++)
        {
            _vecs[i] = new Vector2i(rand.Next(-100, 100), rand.Next(-100, 100));
            _dirs[i] = (AtmosDirection)(1 << rand.Next(4));
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public void OffsetOld()
    {
        for (var i = 0; i < Count; i++)
        {
            _vecs[i] = OffsetOld(_vecs[i], _dirs[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void OffsetNew1()
    {
        for (var i = 0; i < Count; i++)
        {
            _vecs[i] = Offset1(_vecs[i], _dirs[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void OffsetNew2()
    {
        for (var i = 0; i < Count; i++)
        {
            _vecs[i] = Offset2(_vecs[i], _dirs[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void OffsetFinal()
    {
        for (var i = 0; i < Count; i++)
        {
            _vecs[i] = _vecs[i].Offset(_dirs[i]);
        }
    }

    public static Vector2i OffsetOld(Vector2i pos, AtmosDirection dir)
    {
        return pos + OldCardinalToIntVec(dir);
    }

    public static Vector2i OldCardinalToIntVec(AtmosDirection dir)
    {
        switch (dir)
        {
            case AtmosDirection.North:
                return new Vector2i(0, 1);
            case AtmosDirection.East:
                return new Vector2i(1, 0);
            case AtmosDirection.South:
                return new Vector2i(0, -1);
            case AtmosDirection.West:
                return new Vector2i(-1, 0);
            default:
                throw new ArgumentException($"Direction dir {dir} is not a cardinal direction", nameof(dir));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i Offset1(Vector2i pos, AtmosDirection dir)
    {
        // offset by bitflags and then sub to get the final offset
        var dx = (dir.IsFlagSet(AtmosDirection.East) ? 1 : 0) - (dir.IsFlagSet(AtmosDirection.West) ? 1 : 0);
        var dy = (dir.IsFlagSet(AtmosDirection.North) ? 1 : 0) - (dir.IsFlagSet(AtmosDirection.South) ? 1 : 0);
        return new Vector2i(pos.X + dx, pos.Y + dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i Offset2(Vector2i pos, AtmosDirection dir)
    {
        // extract individual bits and compute delta = (positive side) - (negative side).
        // literally 5% faster than .IsFlagSet
        // works on my machine award
        var b = (byte)dir;
        var dx = ((b >> 2) & 1) - ((b >> 3) & 1);
        var dy = ((b >> 0) & 1) - ((b >> 1) & 1);

        return new Vector2i(pos.X + dx, pos.Y + dy);
    }
}
