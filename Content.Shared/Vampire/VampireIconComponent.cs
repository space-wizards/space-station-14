using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VampireIconComponent : Component
{
    [DataField("vampireStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VampireFaction";
}