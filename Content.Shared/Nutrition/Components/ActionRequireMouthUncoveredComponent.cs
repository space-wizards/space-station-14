using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IngestionSystem))]
public sealed partial class ActionRequireMouthUncoveredComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.HEAD | SlotFlags.MASK;
}
