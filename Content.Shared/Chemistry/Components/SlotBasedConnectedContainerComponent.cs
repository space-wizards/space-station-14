using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Component for marking linked container in character slot, to which entity is bound.
/// </summary>
[RegisterComponent]
public sealed partial class SlotBasedConnectedContainerComponent : Component
{
    /// <summary>
    /// The slot in which target container should be.
    /// </summary>
    [DataField("targetSlot", required: true)]
    public SlotFlags TargetSlot;

    /// <summary>
    /// A whitelist for determining whether or not an container is valid.
    /// </summary>
    [DataField("providerWhitelist")]
    public EntityWhitelist? ContainerWhitelist;
}
