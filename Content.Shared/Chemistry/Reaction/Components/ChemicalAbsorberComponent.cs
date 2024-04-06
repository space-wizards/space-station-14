using Content.Shared.Chemistry.Reaction.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChemicalAbsorberComponent : Component
{
    [DataField]
    public TimeSpan LastUpdate;

    /// <summary>
    /// List of absorption groups, these will be split into single/multi-reagent absorptions and then sorted by priortiy
    /// with multi-reagent absorptions being checked FIRST.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AbsorptionGroupPrototype>> AbsorptionGroups = new();

    /// <summary>
    /// A list of individual absorptions to add in addition to the ones contained in the absorption groups
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AbsorptionPrototype>>? AdditionalAbsorptions = null;

    /// <summary>
    /// Multiplier for the absorption rate of the chosen absorption (if any)
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 RateMultiplier = 1.0;

    /// <summary>
    /// List of all the reagent absorption reactions in the order they should be checked
    /// </summary>
    public List<CachedAbsorptionData> CachedAbsorptionOrder = new();
}

[DataDefinition]
public partial struct CachedAbsorptionData
{
    public List<(ProtoId<ReagentPrototype>, FixedPoint2)> RequiredReagents;
    public List<(ProtoId<ReagentPrototype>, FixedPoint2)>? RequiredCatalysts;
    public FixedPoint2 MinTemp;
    public FixedPoint2 MaxTemp;
    public readonly float Rate;
    public bool Quantized;
    public ProtoId<AbsorptionPrototype> ProtoId;

    public CachedAbsorptionData(List<(ProtoId<ReagentPrototype>, FixedPoint2)> requiredReagents,
        List<(ProtoId<ReagentPrototype>, FixedPoint2)>? requiredCatalysts,
        AbsorptionPrototype absorptionProto)
    {
        RequiredReagents = requiredReagents;
        RequiredCatalysts = requiredCatalysts;
        Quantized = absorptionProto.Quantized;
        MinTemp = absorptionProto.MinTemp;
        MaxTemp = absorptionProto.MaxTemp;
        Rate = absorptionProto.Rate;
        ProtoId = absorptionProto.ID;
    }
};
