using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IngestionSystem))]
public sealed partial class ActionRequireMouthUncoveredComponent : Component
{
    /// <summary>
    /// The slots that must be free for the action to be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.HEAD | SlotFlags.MASK;
}
