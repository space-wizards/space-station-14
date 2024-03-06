using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Enables a shuttle/pod to travel to a destination with an item inserted
/// </summary>
[RegisterComponent]
public sealed partial class SharedShuttleDestinationSlotComponent : Component
{
    [DataField]
    public ItemSlot DiskSlot = new();

    [DataField]
    public string DiskSlotId = "Disk";
}
