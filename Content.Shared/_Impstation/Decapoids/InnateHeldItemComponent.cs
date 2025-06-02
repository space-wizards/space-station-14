using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared.Decapoids.Components;

[RegisterComponent]
public sealed partial class InnateHeldItemComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId<ItemComponent> ItemPrototype;
}
