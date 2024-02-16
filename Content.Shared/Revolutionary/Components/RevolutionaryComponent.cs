using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Used for marking regular revs as well as storing icon prototypes so you can see fellow revs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class RevolutionaryComponent : Component, IAntagStatusIconComponent
{
    /// <summary>
    /// The status icon prototype displayed for revolutionaries
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "RevolutionaryFaction";

    public override bool SessionSpecific => true;

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
