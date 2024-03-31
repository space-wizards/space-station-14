using Robust.Shared.GameStates;

namespace Content.Shared.Inventory.VirtualItem;

/// <inheritdoc cref="SharedVirtualItemSystem"/>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class VirtualItemComponent : Component
{
    /// <summary>
    /// The entity blocking this slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid BlockingEntity;
}
