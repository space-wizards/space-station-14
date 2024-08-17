using System.Collections;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Collections;

namespace Content.Shared.Chemistry.Reagent;

public struct SolutionContents : IEnumerable<ReagentQuantity>
{
    public FixedPoint2 Volume { get; private set; }= 0;

    public int Count => _contents.Count;
    private float _temperature = Atmospherics.T20C;
    public float Temperature
    {
        get => _temperature;
        set
        {
            if (!HasTemperature)
                HasTemperature = true;
            _temperature = value;
        }
    }

    public bool HasTemperature = false;
    private ValueList<ReagentQuantity> _contents = new(0);

    public SolutionContents(int count)
    {
        _contents = new(count);
    }
    public SolutionContents(params ReagentQuantity[] reagents)
    {
        _contents = new(reagents.Length);
        foreach (var quant in reagents)
        {
            _contents.Add(quant);
        }
    }

    public SolutionContents(float temperature, params ReagentQuantity[] reagents) : this(reagents)
    {
        _temperature = temperature;
        HasTemperature = true;
    }

    public SolutionContents(ICollection<ReagentQuantity> reagents, float temperature = Atmospherics.T20C)
    {
        _temperature = temperature;
        _contents = new(reagents.Count);
        foreach (var quant in reagents)
        {
            _contents.Add(quant);
        }
    }

    public ReagentQuantity this[int i]
    {
        get => _contents[i];
        set
        {
            if (value.Quantity == 0)
            {
                Remove(value.ReagentDef);
                return;
            }
            var delta = value.Quantity - _contents[i].Quantity ;
            _contents[i] = value;
            Volume += delta;
            ClampTotal();
        }
    }

    public ReagentQuantity this[ReagentDef reagent]
    {
        get
        {
            if (!TryGetReagentIndex(reagent, out var index))
                throw new KeyNotFoundException($"{reagent} could not be found in solutionContents");
            return _contents[index];
        }
        set
        {
            if (!TryGetReagentIndex(reagent, out var index))
            {
                _contents[index] = value;
                return;
            }
            _contents.Add(value);
            Volume += value.Quantity;
            ClampTotal();
        }
    }

    public bool ContainsReagent(ReagentDef def)
    {
        return TryGetReagent(def, out _);
    }

    public ReagentQuantity? GetReagent(ReagentDef reagent)
    {
        if (!TryGetReagent(reagent, out var quantity))
            return null;
        return quantity;
    }

    public bool TryGetReagent(ReagentDef reagent, out ReagentQuantity quantity)
    {
        if (reagent.IsValid)
        {
            foreach (var reagentQuant in _contents)
            {
                quantity = reagentQuant;
                if (reagent == reagentQuant)
                    return true;
            }
        }
        quantity = ReagentQuantity.Invalid;
        return false;
    }

    public int IndexOfReagent(ReagentDef reagent)
    {
        TryGetReagentIndex(reagent, out var index);
        return index;
    }

    public bool TryGetReagentIndex(ReagentDef reagent, out int index)
    {
        index = -1;
        if (reagent.IsValid)
            return false;
        for (index = 0; index < _contents.Count; index++)
        {
            var reagentQuant = _contents[index];
            if (reagent == reagentQuant)
                return true;
        }
        return false;
    }

    public void Add(ReagentQuantity quantity)
    {
        Add(quantity,quantity);
    }

    public void Remove(ReagentQuantity quantity)
    {
        Add(quantity,- quantity.Quantity );
    }

    public void Set(ReagentQuantity quantity)
    {
        if (!quantity.IsValid)
            return;
        var index = IndexOfReagent(quantity);
        if (index < 0)
            return;
        this[index] = quantity;
    }

    private void Add(ReagentDef reagentDef, FixedPoint2 quantity)
    {
        if (!reagentDef.IsValid)
            return;
        var i = 0;
        foreach (ref var foundQuant in _contents.Span)
        {
            if (reagentDef== foundQuant.ReagentDef)
            {
                foundQuant.Quantity += quantity;
                Volume += quantity;
                ClampTotal();
                if (foundQuant.Quantity < 0)
                {
                    _contents.RemoveAt(i);
                    return;
                }
            }
            i++;
        }
    }

    private void ClampTotal()
    {
        if (Volume < 0)
        {
            Volume = 0;
            _contents.Clear();
        }
    }

    public void Remove(ReagentDef reagent)
    {
        var i = 0;
        foreach (ref var foundQuant in _contents.Span)
        {
            if (reagent == foundQuant.ReagentDef)
            {
                Volume -= foundQuant.Quantity;
                ClampTotal();
                if (foundQuant.Quantity < 0)
                {
                    _contents.RemoveAt(i);
                    return;
                }
            }
            i++;
        }
    }

    public void Scale(float scale)
    {
        if (scale < 0)
            return;
        if (scale == 0)
        {
            _contents.Clear();
            Volume = 0;
        }
        foreach (ref var reagents in _contents.Span)
        {
            reagents.Quantity *= scale;
        }

        Volume *= scale;
    }

    public static implicit operator ReagentQuantity[](SolutionContents s) => s._contents.ToArray();

    [MustDisposeResource]
    public IEnumerator<ReagentQuantity> GetEnumerator()
    {
        return _contents.GetEnumerator();
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
