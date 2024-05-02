using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traitor.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodBrotherComponent : Component, IAntagStatusIconComponent
{
    /// <summary>
    /// The status icon prototype displayed for revolutionaries
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "BloodBrotherFaction";

    public override bool SessionSpecific => true;

    [DataField]
    public bool IconVisibleToGhost { get; set; } = false;
}
