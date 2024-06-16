using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traitor.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(BloodBrotherSystem))]
public sealed partial class SharedBloodBrotherComponent : Component, IAntagStatusIconComponent
{
    /// <summary>
    /// The status icon prototype displayed for blood brothers
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "BloodBrotherFaction";

    public string TeamID = string.Empty;

    public override bool SessionSpecific => true;

    [DataField]
    public bool IconVisibleToGhost { get; set; } = false;
}
