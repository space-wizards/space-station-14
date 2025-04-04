using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

// Added to entities that are petrified aka turned into stone, this is on the new stone statue body after polymorph
[RegisterComponent, NetworkedComponent]
public sealed partial class PetrifiedStatueComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototypeName = "AnimateStoneStatue";
}
