using Content.Server.Shuttles.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttle.Components;

/// <summary>
/// Enables a shuttle/pod to travel to a destination with an item inserted
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleDestinationSlotComponent : Component
{
    [DataField]
    public ItemSlot DiskSlot = new();

    [DataField]
    public string DiskSlotId = "Coordinate Disk";
}
