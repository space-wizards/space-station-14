using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Metabolism.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Metabolism.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CaloricStorageComponent : Component
{

    /// <summary>
    /// What type of metabolism is used
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetabolismTypePrototype> MetabolismType;

    /// <summary>
    /// Short-term stored energy in KiloCalories (KCal)
    /// This simulates the body's glycogen storage, and serves as a buffer before long-term storage is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FastStorage = 1500;

    /// <summary>
    /// Longer term stored energy in KiloCalories (KCal)
    /// This simulates the body's fat/adipose storage, and serves as a long term fall back if all glycogen is used up.
    /// If this is completely used up, metabolism stops and organs start dying
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 LongStorage = 80000;

    [DataField, AutoNetworkedField]
    public ReagentId CachedReagent;

    [DataField, AutoNetworkedField]
    public float KCalPerReagent;
}
