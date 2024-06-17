using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.BloodBrother.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedBloodBrotherSystem))]
public sealed partial class SharedBloodBrotherComponent : Component
{
    /// <summary>
    ///     The status icon prototype displayed for blood brothers
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "BloodBrotherFaction";

    public string TeamID = string.Empty;

    // will be used as shared objectives
    public List<EntityUid> Objectives;

    public override bool SessionSpecific => true;

    [DataField]
    public bool IconVisibleToGhost { get; set; } = false;
}
