using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Zombies;

[RegisterComponent, NetworkedComponent]
public sealed partial class InitialInfectedComponent : Component, IAntagStatusIconComponent
{
    [DataField("initialInfectedStatusIcon")]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "InitialInfectedFaction";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
