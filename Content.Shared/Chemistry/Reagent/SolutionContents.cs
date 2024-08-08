using System.Runtime.InteropServices;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[Serializable, NetSerializable, DataDefinition]
public partial struct SolutionContents
{
    [DataField(required:true)]
    public List<ReagentQuantity> Reagents { get; private set; }= new(SharedSolutionSystem.SolutionAlloc);

    [DataField("temp")]
    public float Temperature = Atmospherics.T20C;

    [DataField("maxVol")]
    public FixedPoint2 MaxVolume = FixedPoint2.MaxValue;

    [DataField]
    public bool CanOverflow = true;

    [DataField]
    public bool CanReact = true;

    [ViewVariables, DataField]
    public FixedPoint2 Volume { get; private set; } = 0;

    [ViewVariables]
    public FixedPoint2 AvailableVolume => MaxVolume - Volume;

    public SolutionContents(List<ReagentQuantity> reagents, FixedPoint2 maxVol,bool canOverflow = true ,
        float temp = Atmospherics.T20C, bool canReact = true)
    {
        MaxVolume = maxVol;
        Temperature = temp;
        CanOverflow = canOverflow;
        Reagents = [..reagents];
        foreach (var (_, quant) in reagents)
        {
            Volume += quant;
        }
    }

    public SolutionContents(List<ReagentQuantity> reagents, bool canOverflow = true, float temp = Atmospherics.T20C,
        bool canReact = true)
        : this(reagents, FixedPoint2.MaxValue, canOverflow, temp, canReact)
    {
    }

    public SolutionContents(bool canOverflow = true, float temp = Atmospherics.T20C, bool canReact = true,
        params ReagentQuantity[] reagents)
        : this([..reagents], FixedPoint2.MaxValue, canOverflow, temp, canReact)
    {
    }

    public SolutionContents(FixedPoint2 maxVol, bool canOverflow = true, float temp = Atmospherics.T20C, bool canReact = true,
        params ReagentQuantity[] reagents) : this([..reagents], maxVol, canOverflow, temp, canReact)
    {
    }

    public SolutionContents(Entity<SolutionComponent> solution, float scaling = 1.0f)
    {
        Reagents = new(SharedSolutionSystem.SolutionAlloc);
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            Reagents.Add(new ReagentQuantity(new (reagentData.ReagentEnt, null), reagentData.Quantity * scaling));
            if (reagentData.Variants == null)
                continue;
            foreach (ref var varData in CollectionsMarshal.AsSpan(reagentData.Variants))
            {
                Reagents.Add(new ReagentQuantity(new (reagentData.ReagentEnt, varData.Variant), varData.Quantity * scaling));
            }
        }
        Temperature = solution.Comp.Temperature;
        MaxVolume = solution.Comp.MaxVolume;
        CanOverflow = solution.Comp.CanOverflow;
        CanReact = solution.Comp.CanReact;
    }

    public bool TryGetReagent(ReagentDef reagent, out FixedPoint2 quantity, bool includeVariants = true)
    {
        FixedPoint2 quant = 0;
        if (includeVariants && reagent.Variant == null)
        {
            foreach (var (id, foundQuant) in Reagents)
            {
                if (reagent.Id != id.Id)
                    continue;
                quant += foundQuant;
            }
        }
        else
        {
            foreach (var (id, foundQuant) in Reagents)
            {
                if (reagent != id)
                    continue;
                quant += foundQuant;
            }
        }
        quantity = quant;
        return true;
    }

    public void SetReagent(ReagentQuantity newQuantity)
    {
        var reagentsSpan = CollectionsMarshal.AsSpan(Reagents);
        var delta = newQuantity.Quantity;
        for (var i = 0; i < Reagents.Count; i++)
        {
            if (reagentsSpan[i].ReagentDef != newQuantity.ReagentDef)
                continue;
            var oldData = reagentsSpan[i];
            delta -= oldData.Quantity;
            Volume += delta;
            reagentsSpan[i] = newQuantity;
            return;
        }
        Reagents.Add(newQuantity);
        Volume += delta;
        if (Volume < 0)
            Volume = 0;
    }

    public void RemoveReagent(ReagentDef reagent, bool includeVariants = true)
    {
        Queue<int> indicesToRemove = new();
        FixedPoint2 delta = 0;
        if (includeVariants && reagent.Variant == null)
        {
            for (var index = 0; index < Reagents.Count; index++)
            {
                var rq = Reagents[index];
                if (rq.ReagentDef.Id != reagent.Id)
                    continue;
                indicesToRemove.Enqueue(index);
                delta -= rq.Quantity;
            }
        }
        else
        {
            for (var index = 0; index < Reagents.Count; index++)
            {
                var rq = Reagents[index];
                if (rq.ReagentDef != reagent)
                    continue;
                indicesToRemove.Enqueue(index);
                delta -= rq.Quantity;
            }
        }

        while (indicesToRemove.Count > 0)
        {
            Reagents.RemoveAt(indicesToRemove.Dequeue());
        }

        Volume += delta;
        if (Volume < 0)
            Volume = 0;
    }

    public void Scale(float factor)
    {
        foreach (ref var quantData in CollectionsMarshal.AsSpan(Reagents))
        {
            quantData.Quantity *= factor;
        }
    }
}

