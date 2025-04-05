using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

// Added to entities that are petrified aka turned into stone, this is on the new stone statue body after polymorph
// This should only be on the petrified version. If the statue is animate, this component shouldn't be.
[RegisterComponent, NetworkedComponent]
public sealed partial class PetrifiedStatueComponent : Component
{
    // Determines what prototype the statue will turn into if animated.
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototypeName = "AnimateStoneStatue";
}
