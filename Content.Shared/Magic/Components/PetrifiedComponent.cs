using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

// Added to entities that are petrified aka turned into stone, this is on the original body before polymorph
[RegisterComponent, NetworkedComponent]
public sealed partial class PetrifiedComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphPrototypeName = "PetrifyStoneStatue";
}
