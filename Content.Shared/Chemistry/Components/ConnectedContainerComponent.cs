using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent]
public sealed partial class ConnectedContainerComponent : Component
{
    /// <summary>
    /// The slot that the ammo provider should be located in.
    /// </summary>
    [DataField("targetSlot", required: true)]
    public SlotFlags TargetSlot;

    /// <summary>
    /// A whitelist for determining whether or not an ammo provider is valid.
    /// </summary>
    [DataField("providerWhitelist")]
    public EntityWhitelist? ContainerWhitelist;
}
