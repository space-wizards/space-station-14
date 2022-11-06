using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Content.Benchmarks;

/// <summary>
///     This benchmark exists to check whether it's better to use lists or dictionaries to store reagents in a solution.
/// </summary>
[SimpleJob]
[Virtual]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SolutionBenchmarks
{
    private sealed class ListSolution
    {
        internal List<Reagent> Contents = new(2);

        internal readonly record struct Reagent(string ReagentId, FixedPoint2 Quantity) { }

        public bool ContainsReagent(string reagentId)
        {
            foreach (var reagent in Contents)
            {
                if (reagent.ReagentId == reagentId)
                    return true;
            }
            return false;
        }

        public void AddReagent(string reagentId, FixedPoint2 quantity)
        {
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;
                Contents[i] = new Reagent(reagentId, reagent.Quantity + quantity);
                return;
            }

            Contents.Add(new Reagent(reagentId, quantity));
        }

        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;
                var newQuantity = curQuantity - quantity;

                if (newQuantity <= 0)
                {
                    Contents.RemoveSwap(i);
                    return curQuantity;
                }

                Contents[i] = new Reagent(reagentId, newQuantity);
                return quantity;
            }
            return FixedPoint2.Zero;
        }

        public ListSolution SplitSolution(FixedPoint2 remainingVolume, FixedPoint2 quantity)
        {
            ListSolution newSolution = new();

            for (var i = Contents.Count - 1; i >= 0; i--)
            {
                var reagent = Contents[i];
                var ratio = (remainingVolume - quantity).Double() / remainingVolume.Double();
                remainingVolume -= reagent.Quantity;

                var newQuantity = reagent.Quantity * ratio;
                var splitQuantity = reagent.Quantity - newQuantity;

                if (newQuantity > 0)
                    Contents[i] = new Reagent(reagent.ReagentId, newQuantity);
                else
                    Contents.RemoveAt(i);

                if (splitQuantity > 0)
                    newSolution.Contents.Add(new Reagent(reagent.ReagentId, splitQuantity));
                quantity -= splitQuantity;
            }
            return newSolution;
        }

        public void AddSolution(ListSolution otherSolution)
        {
            for (var i = 0; i < otherSolution.Contents.Count; i++)
            {
                var otherReagent = otherSolution.Contents[i];

                var found = false;
                for (var j = 0; j < Contents.Count; j++)
                {
                    var reagent = Contents[j];
                    if (reagent.ReagentId == otherReagent.ReagentId)
                    {
                        found = true;
                        Contents[j] = new Reagent(reagent.ReagentId, reagent.Quantity + otherReagent.Quantity);
                        break;
                    }
                }

                if (!found)
                {
                    Contents.Add(new Reagent(otherReagent.ReagentId, otherReagent.Quantity));
                }
            }
        }

        public ListSolution Clone()
        {
            var volume = FixedPoint2.New(0);
            var newSolution = new ListSolution();
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                newSolution.Contents.Add(reagent);
                volume += reagent.Quantity;
            }
            return newSolution;
        }
    }


    private sealed class IntListSolution
    {
        internal List<IntReagent> Contents = new(2);

        internal readonly record struct IntReagent(int ReagentId, FixedPoint2 Quantity) { }

        public bool ContainsReagent(int reagentId)
        {
            foreach (var reagent in Contents)
            {
                if (reagent.ReagentId == reagentId)
                    return true;
            }
            return false;
        }

        public void AddReagent(int reagentId, FixedPoint2 quantity)
        {
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;
                Contents[i] = new IntReagent(reagentId, reagent.Quantity + quantity);
                return;
            }

            Contents.Add(new IntReagent(reagentId, quantity));
        }

        public FixedPoint2 RemoveReagent(int reagentId, FixedPoint2 quantity)
        {
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;
                var newQuantity = curQuantity - quantity;

                if (newQuantity <= 0)
                {
                    Contents.RemoveSwap(i);
                    return curQuantity;
                }

                Contents[i] = new IntReagent(reagentId, newQuantity);
                return quantity;
            }
            return FixedPoint2.Zero;
        }

        public IntListSolution SplitSolution(FixedPoint2 remainingVolume, FixedPoint2 quantity)
        {
            IntListSolution newSolution = new();

            for (var i = Contents.Count - 1; i >= 0; i--)
            {
                var reagent = Contents[i];
                var ratio = (remainingVolume - quantity).Double() / remainingVolume.Double();
                remainingVolume -= reagent.Quantity;

                var newQuantity = reagent.Quantity * ratio;
                var splitQuantity = reagent.Quantity - newQuantity;

                if (newQuantity > 0)
                    Contents[i] = new IntReagent(reagent.ReagentId, newQuantity);
                else
                    Contents.RemoveAt(i);

                if (splitQuantity > 0)
                    newSolution.Contents.Add(new IntReagent(reagent.ReagentId, splitQuantity));
                quantity -= splitQuantity;
            }
            return newSolution;
        }

        public void AddSolution(IntListSolution otherSolution)
        {
            for (var i = 0; i < otherSolution.Contents.Count; i++)
            {
                var otherReagent = otherSolution.Contents[i];

                var found = false;
                for (var j = 0; j < Contents.Count; j++)
                {
                    var reagent = Contents[j];
                    if (reagent.ReagentId == otherReagent.ReagentId)
                    {
                        found = true;
                        Contents[j] = new IntReagent(reagent.ReagentId, reagent.Quantity + otherReagent.Quantity);
                        break;
                    }
                }

                if (!found)
                {
                    Contents.Add(new IntReagent(otherReagent.ReagentId, otherReagent.Quantity));
                }
            }
        }

        public IntListSolution Clone()
        {
            var volume = FixedPoint2.New(0);
            var newSolution = new IntListSolution();
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                newSolution.Contents.Add(reagent);
                volume += reagent.Quantity;
            }
            return newSolution;
        }
    }


    private sealed class DictionarySolution
    {
        public Dictionary<string, FixedPoint2> Contents = new(2);

        public bool ContainsReagent(string reagentId) => Contents.ContainsKey(reagentId);

        public FixedPoint2 AddReagent(string id, FixedPoint2 quantity)
        {
            Contents[id] = Contents.TryGetValue(id, out var existing)
                ? quantity + existing
                : quantity;

            return quantity;
        }

        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            if (!Contents.TryGetValue(reagentId, out var existing))
                return FixedPoint2.Zero;

            if (quantity >= existing)
            {
                Contents.Remove(reagentId);
                return existing;
            }

            Contents[reagentId] = existing - quantity;
            return quantity;
        }

        public DictionarySolution SplitSolution(FixedPoint2 remaining, FixedPoint2 toTake)
        {
            DictionarySolution newSolution;
            newSolution = new()
            {
                Contents = new(Contents.Count),
            };

            foreach (var (id, quantity) in Contents)
            {
                var taken = quantity * toTake / remaining;
                if (taken == FixedPoint2.Zero)
                    continue;

                remaining -= taken;
                newSolution.Contents[id] = taken;
                if (quantity == taken)
                    Contents.Remove(id);
                else
                    Contents[id] = quantity - taken;
            }

            return newSolution;
        }

        public void AddSolution(DictionarySolution otherSolution)
        {
            foreach (var (id, quantity) in otherSolution.Contents)
            {
                Contents[id] = Contents.TryGetValue(id, out var existing)
                    ? quantity + existing
                    : quantity;
            }
        }

        public DictionarySolution Clone()
        {
            return new DictionarySolution()
            {
                Contents = Contents.ShallowClone()
            };
        }
    }


    private sealed class IntDictionarySolution
    {
        public Dictionary<int, FixedPoint2> Contents = new(2);

        public bool ContainsReagent(int reagentId) => Contents.ContainsKey(reagentId);

        public FixedPoint2 AddReagent(int id, FixedPoint2 quantity)
        {
            Contents[id] = Contents.TryGetValue(id, out var existing)
                ? quantity + existing
                : quantity;

            return quantity;
        }

        public FixedPoint2 RemoveReagent(int reagentId, FixedPoint2 quantity)
        {
            if (!Contents.TryGetValue(reagentId, out var existing))
                return FixedPoint2.Zero;

            if (quantity >= existing)
            {
                Contents.Remove(reagentId);
                return existing;
            }

            Contents[reagentId] = existing - quantity;
            return quantity;
        }

        public IntDictionarySolution SplitSolution(FixedPoint2 remaining, FixedPoint2 toTake)
        {
            IntDictionarySolution newSolution;
            newSolution = new()
            {
                Contents = new(Contents.Count),
            };

            foreach (var (id, quantity) in Contents)
            {
                var taken = quantity * toTake / remaining;
                if (taken == FixedPoint2.Zero)
                    continue;

                remaining -= taken;
                newSolution.Contents[id] = taken;
                if (quantity == taken)
                    Contents.Remove(id);
                else
                    Contents[id] = quantity - taken;
            }

            return newSolution;
        }

        public void AddSolution(IntDictionarySolution otherSolution)
        {
            foreach (var (id, quantity) in otherSolution.Contents)
            {
                Contents[id] = Contents.TryGetValue(id, out var existing)
                    ? quantity + existing
                    : quantity;
            }
        }

        public IntDictionarySolution Clone()
        {
            return new IntDictionarySolution()
            {
                Contents = Contents.ShallowClone()
            };
        }
    }

    // Conclusion:
    // = When adding, removing, cloning, splitting etc lists are better than dictionaries up until you have MANY reagents in a solution.
    // - As expected, int keys is better than dict keys, but the need to do mapping slows them down a lot. Fortunately most systems could be refactored to use int keys as an input. Not sure about reactions.
    // - Dictionary contains/try-get checks are noticeable faser, which maybe matters of reactions?.

    private ListSolution[] _lists = new ListSolution[100];
    private IntListSolution[] _intlists = new IntListSolution[100];
    private DictionarySolution[] _dicts = new DictionarySolution[100];
    private IntDictionarySolution[] _intdicts = new IntDictionarySolution[100];
    private string[] _stringIds = new string[100];
    private int[] _intIds = new int[100];
    private FixedPoint2[] _quantities = new FixedPoint2[100];
    private FixedPoint2[] _totalQuantities = new FixedPoint2[100];
    private string[] _reagentPrototypes = new string[10] { "lorem", "ipsum", "dolor", "consectetur", "adipiscing", "eiusmod", "tempor", "incididunt", "labore", "dolore" };
    private readonly Random _random = new();
    private Dictionary<string, int> _keyMap = new();

    [GlobalSetup]
    public void Setup()
    {
        // Each solution will contain 1-5 of 10 possible reagents.
        for (var i = 0; i < 100; i++)
        {
            var count = 1 + _random.Next(5);
            _lists[i] = new();
            _dicts[i] = new();
            _intdicts[i] = new();
            _intlists[i] = new();
            _totalQuantities[i] = 0;
            for (var j = 0; j < count; j++)
            {
                var id = _random.Next(_reagentPrototypes.Length);
                var reagent = _reagentPrototypes[id];
                var quantity = FixedPoint2.New(_random.Next(100) + 1);
                _totalQuantities[i] += quantity;
                _lists[i].AddReagent(reagent, quantity);
                _dicts[i].AddReagent(reagent, quantity);
                _intdicts[i].AddReagent(id, quantity);
                _intlists[i].AddReagent(id, quantity);
            }
            var intId = _random.Next(_reagentPrototypes.Length);
            _intIds[i] = intId;
            _stringIds[i] = _reagentPrototypes[intId];
            _quantities[i] = FixedPoint2.New(_random.Next(100) + 1);
        }

        for (var i = 0; i < _reagentPrototypes.Length; i++)
        {
            _keyMap.Add(_reagentPrototypes[i], i);
        }
    }

    /// <summary>
    ///     Baseline existing implementation
    /// </summary>
    [BenchmarkCategory("Contains")]
    [Benchmark(Baseline = true)]
    public void ListContains()
    {
        for (var i = 0; i < 100; i++)
        {
            _lists[i].ContainsReagent(_stringIds[i]);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with string keys.
    /// </summary>
    [BenchmarkCategory("Contains")]
    [Benchmark]
    public void DictContains()
    {
        for (var i = 0; i < 100; i++)
        {
            _dicts[i].ContainsReagent(_stringIds[i]);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with integer keys.
    /// </summary>
    [BenchmarkCategory("Contains")]
    [Benchmark]
    public void IntDictContains()
    {
        for (var i = 0; i < 100; i++)
        {
            _intdicts[i].ContainsReagent(_intIds[i]);
        }
    }

    /// <summary>
    ///     Alternative using lists with integer keys.
    /// </summary>
    [BenchmarkCategory("Contains")]
    [Benchmark]
    public void IntListContains()
    {
        for (var i = 0; i < 100; i++)
        {
            _intlists[i].ContainsReagent(_intIds[i]);
        }
    }


    /// <summary>
    ///     Baseline existing implementation
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark(Baseline = true)]
    public void ListAddRemoveReagent()
    {
        for (var i = 0; i < 100; i++)
        {
            _lists[i].AddReagent(_stringIds[i], _quantities[i]);
            _lists[i].RemoveReagent(_stringIds[i], _quantities[i]);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with string keys.
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark]
    public void DictAddRemoveReagent()
    {
        // implementatin with dictionaries
        for (var i = 0; i < 100; i++)
        {
            _dicts[i].AddReagent(_stringIds[i], _quantities[i]);
            _dicts[i].RemoveReagent(_stringIds[i], _quantities[i]);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with integer keys.
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark]
    public void IntDictAddRemoveReagent()
    {
        for (var i = 0; i < 100; i++)
        {
            _intdicts[i].AddReagent(_intIds[i], _quantities[i]);
            _intdicts[i].RemoveReagent(_intIds[i], _quantities[i]);
        }
    }

    /// <summary>
    ///     Alternative using lists with integer keys.
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark]
    public void IntListAddRemoveReagent()
    {
        for (var i = 0; i < 100; i++)
        {
            _intlists[i].AddReagent(_intIds[i], _quantities[i]);
            _intlists[i].RemoveReagent(_intIds[i], _quantities[i]);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with integer keys, but assuming we need to convert from a string id to int before adding.
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark]
    public void IntMappedDictAddRemoveReagent()
    {
        for (var i = 0; i < 100; i++)
        {
            _intdicts[i].AddReagent(_keyMap[_stringIds[i]], _quantities[i]);
            _intdicts[i].RemoveReagent(_keyMap[_stringIds[i]], _quantities[i]);
        }
    }

    /// <summary>
    ///     Alternative using lists with integer keys, but assuming we need to convert from a string id to int before adding.
    /// </summary>
    [BenchmarkCategory("Add-Remove Reagent")]
    [Benchmark]
    public void IntMappedListAddRemoveReagent()
    {
        for (var i = 0; i < 100; i++)
        {
            _intlists[i].AddReagent(_keyMap[_stringIds[i]], _quantities[i]);
            _intlists[i].RemoveReagent(_keyMap[_stringIds[i]], _quantities[i]);
        }
    }

    /// <summary>
    ///     Baseline existing implementation
    /// </summary>
    [BenchmarkCategory("Clone-merge-split")]
    [Benchmark(Baseline = true)]
    public void ListCloneMergeSplit()
    {
        for (var i = 0; i < 100; i++)
        {
            var a = _totalQuantities[i];
            var sol = _lists[i].Clone();
            sol.AddSolution(_lists[99 - i]);
            sol.SplitSolution(2 * a, a);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with string keys.
    /// </summary>
    [BenchmarkCategory("Clone-merge-split")]
    [Benchmark]
    public void DictCloneMergeSplit()
    {
        for (var i = 0; i < 100; i++)
        {
            var a = _totalQuantities[i];
            var sol = _dicts[i].Clone();
            sol.AddSolution(_dicts[99 - i]);
            sol.SplitSolution(2 * a, a);
        }
    }

    /// <summary>
    ///     Alternative using dictionaries with integer keys.
    /// </summary>
    [BenchmarkCategory("Clone-merge-split")]
    [Benchmark]
    public void IntDictCloneMergeSplit()
    {
        for (var i = 0; i < 100; i++)
        {
            var a = _totalQuantities[i];
            var sol = _intdicts[i].Clone();
            sol.AddSolution(_intdicts[99 - i]);
            sol.SplitSolution(2 * a, a);
        }
    }

    /// <summary>
    ///     Alternative using lists with integer keys.
    /// </summary>
    [BenchmarkCategory("Clone-merge-split")]
    [Benchmark]
    public void IntListCloneMergeSplit()
    {
        for (var i = 0; i < 100; i++)
        {
            var a = _totalQuantities[i];
            var sol = _intlists[i].Clone();
            sol.AddSolution(_intlists[99 - i]);
            sol.SplitSolution(2 * a, a);
        }
    }
}
