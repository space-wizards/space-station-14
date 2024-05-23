using Content.Shared.Chemistry.Reaction.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

[Prototype]
public sealed partial class AbsorptionPrototype : BaseReactionPrototype, IPrototype
{

    /// <summary>
    /// Will this reagent be transferred to a target solution if possible, or will it always be deleted on absorption?
    /// </summary>
    [DataField] public bool CanTransfer = false;

    public bool IsComboAbsorption => Catalysts is {Count: > 0} || Reactants.Count > 1;

    public AbsorptionReaction GetData(float multiplier = 0f)
    {
        var reagentsList = new List<(ProtoId<ReagentPrototype>, FixedPoint2)>();
        foreach (var (key, value) in Reactants)
        {
                reagentsList.Add((key, value * multiplier));
        }
        List<(ProtoId<ReagentPrototype>, FixedPoint2)>? catalystList = null;
        if (Catalysts != null)
        {
            catalystList = new List<(ProtoId<ReagentPrototype>, FixedPoint2)>();
            foreach (var (key, value) in Catalysts)
            {
                catalystList.Add((key, value * multiplier));
            }
        }
        return new(
            Rate,
            Priority,
            reagentsList,
            catalystList,
            Quantized,
            MinTemp,
            MaxTemp,
            TransferHeat,
            ID,
            CanTransfer,
            Impact,
            Conditions,
            Effects,
            ReagentEffects,
            Sound);
    }
}

[DataRecord, NetSerializable]
public record struct AbsorptionReaction(
    float Rate,
    int Priority,
    List<(ProtoId<ReagentPrototype>, FixedPoint2)> Reactants,
    List<(ProtoId<ReagentPrototype>, FixedPoint2)>? Catalysts,
    bool Quantized,
    float MinTemp,
    float MaxTemp,
    bool TransferHeat,
    string ProtoId,
    bool CanTransfer,
    LogImpact? Impact,
    List<ChemicalCondition>? Conditions,
    List<ChemicalEffect>? Effects,
    List<ReagentEffect>? ReagentEffects,
    SoundSpecifier? Sound) : IReactionData
{
    public int CompareTo(IReactionData? other)
    {
        if (other == null)
            return -1;

        if (Priority != other.Priority)
            return other.Priority - Priority;

        return string.Compare(ProtoId, other.ProtoId, StringComparison.Ordinal);
    }
}

