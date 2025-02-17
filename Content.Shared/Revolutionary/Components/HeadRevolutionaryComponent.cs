using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Component used for marking a Head Rev for conversion and winning/losing.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class HeadRevolutionaryComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the head revolutionary.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "HeadRevolutionaryFaction";

    /// <summary>
    /// How long the stun will last after the user is converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(8);

    [DataField("headRevConvertAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HeadRevConvertAction = "ActionHeadRevConvert";

    [DataField("actionEntity")] public EntityUid HeadRevConvertActionEntity;

    public override bool SessionSpecific => true;

    [DataField]
    public bool MassacreStage = false;
}
