using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadableComponent : Component
{
    [ViewVariables]
    [NonSerialized]
    public List<Entity<RailroadCardComponent, RuleOwnerComponent>>? IssuedCards;

    [ViewVariables]
    [NonSerialized]
    public Entity<RailroadCardComponent, RuleOwnerComponent>? ActiveCard;

    [ViewVariables]
    [NonSerialized]
    public List<Entity<RailroadCardComponent, RuleOwnerComponent>>? Completed;

    [DataField]
    [NonSerialized]
    public bool Restricted = false;
}
