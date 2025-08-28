using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.ScanGate;

[ByRefEvent]
public record struct TryDetectItem(EntityUid ScanGate, bool EntityDetected = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.All; // Detect everything
}

[Serializable, NetSerializable]
public enum ScanGateVisuals
{
    State
}

public enum ScanGateVisualLayers : byte
{
    Status
}