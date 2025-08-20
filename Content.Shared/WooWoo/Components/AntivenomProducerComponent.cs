using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.WooWoo.Systems.Antivenom;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.WooWoo.Components.Antivenom;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedAntivenomProducerSystem))]
public sealed partial class AntivenomProducerComponent : Component
{
    /// <summary>
    /// venom antivenom pairs and thresholds declared on the prototype.
    /// The system can build lookups from this at runtime.
    /// </summary>
    [DataField(required: true)]
    public List<AntivenomImmunityConfig> ImmunityConfigs = new();

    /// <summary>
    /// How much of each venom has been metabolized historically (units).
    /// Used to determine when an immunity unlocks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> MetabolizedTotals = new();

    /// <summary>
    /// Which venoms have reached their immunity threshold, and the immunity stage (for multiples).
    /// We keep this separate from MetabolizedTotals for a crisp “immune/not yet” state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, uint> UnlockedImmunities = new();

    /// <summary>
    /// If true, production is currently paused (stasis, dead, husk, etc.).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ImmunoCompromised = false;

    // Cached lookups

    /// <summary>
    /// venom to config lookup.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, AntivenomImmunityConfig> ConfigByVenom = new();

    /// <summary>
    /// antivenom to config lookup.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, AntivenomImmunityConfig> ConfigByAntivenom = new();
}

/// <summary>
/// A data-driven rule describing how exposure to a specific venom unlocks production
/// of a corresponding antivenom, at some rate, with optional decay and caps.
/// </summary>
[DataDefinition]
public partial record struct AntivenomImmunityConfig
{
    /// <summary>
    /// The venom reagent prototype ID the body learns to counter.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public ProtoId<ReagentPrototype> Venom;

    /// <summary>
    /// The antivenom reagent prototype ID produced once the venom threshold is reached.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public ProtoId<ReagentPrototype> Antivenom;

    /// <summary>
    /// Units of venom that must be metabolized (cumulative) to unlock the immunity.
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = FixedPoint2.New(50);

    /// <summary>
    /// Units produced per stage per update once unlocked.
    /// </summary>
    [DataField]
    public FixedPoint2 AVPerStage = FixedPoint2.New(1);

    /// <summary>
    /// The highest attainable stage of immunity. multiplicative with AV creation.
    /// </summary>
    [DataField]
    public uint MaxStage;

    public AntivenomImmunityConfig(
        ProtoId<ReagentPrototype> venom,
        ProtoId<ReagentPrototype> antivenom,
        FixedPoint2 threshold,
        FixedPoint2 aVPerStage,
        uint maxStage
        )
    {
        Venom = venom;
        Antivenom = antivenom;
        Threshold = threshold;
        AVPerStage = aVPerStage;
        MaxStage = maxStage;
    }
}

