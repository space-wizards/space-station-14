using Content.Shared.Chemistry.Reaction.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reaction.Prototypes;


//Basic implementation of rate limited reaction prototype primarily for digestion/medcode code to not suck ass.

[Prototype]
public sealed partial class RateReactionPrototype : BaseReactionPrototype, IPrototype
{

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField("requiredMixerCategories")]
    public List<ProtoId<MixingCategoryPrototype>>? MixingCategories = null;

    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Products = new();

    /// <summary>
    /// Determines whether or not this reaction creates a new chemical (false) or if it's a breakdown for existing chemicals (true)
    /// Used in the chemistry guidebook to make divisions between recipes and reaction sources.
    /// </summary>
    /// <example>
    /// Mixing together two reagents to get a third -> false
    /// Heating a reagent to break it down into 2 different ones -> true
    /// </example>
    [DataField]
    public bool Source;

    public RateReaction Data
    {
        get
        {
            var reactantsList = new List<(ProtoId<ReagentPrototype>, FixedPoint2)>();
            foreach (var (key, value) in Reactants)
            {
                reactantsList.Add((key, value));
            }
            var productsList = new List<(ProtoId<ReagentPrototype>, FixedPoint2)>();
            foreach (var (key, value) in Products)
            {
                productsList.Add((key, value));
            }
            List<(ProtoId<ReagentPrototype>, FixedPoint2)>? catalystList = null;
            if (Catalysts != null)
            {
                catalystList = new List<(ProtoId<ReagentPrototype>, FixedPoint2)>();
                foreach (var (key, value) in Catalysts)
                {
                    catalystList.Add((key, value));
                }
            }
            return new(
                Rate,
                Priority,
                reactantsList,
                productsList,
                catalystList,
                Quantized,
                MinTemp,
                MaxTemp,
                TransferHeat,
                ID,
                Impact,
                Conditions,
                Effects,
                ReagentEffects,
                Sound);
        }
    }
}

[DataRecord, NetSerializable]
public record struct RateReaction(
    float Rate,
    int Priority,
    List<(ProtoId<ReagentPrototype>, FixedPoint2)> Reactants,
    List<(ProtoId<ReagentPrototype>, FixedPoint2)> Products,
    List<(ProtoId<ReagentPrototype>, FixedPoint2)>? Catalysts,
    bool Quantized,
    float MinTemp,
    float MaxTemp,
    bool TransferHeat,
    string ProtoId,
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
