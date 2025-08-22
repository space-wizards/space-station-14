using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Ronstation.Vampire.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VampireComponent : Component
{
    // [DataField]
    // public ProtoId<FactionIconPrototype> VampireIcon = "VampireFactionIcon";

    public override bool SessionSpecific => true;

}