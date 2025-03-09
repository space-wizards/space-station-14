using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

// Added to entities that are petrified aka turned into stone
[RegisterComponent, NetworkedComponent]
public sealed partial class PetrifiedComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototypeName = "PetrifyStoneStatue";
}
