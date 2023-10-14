using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

namespace Content.Shared.Payload.Components;

/// <summary>
///     Chemical payload that mixes the solutions of two drain-able solution containers when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class ChemicalPayloadComponent : Component
{
    [DataField("beakerSlotA", required: true)]
    public ItemSlot BeakerSlotA = new();

    [DataField("beakerSlotB", required: true)]
    public ItemSlot BeakerSlotB = new();
}

[Serializable, NetSerializable]
public enum ChemicalPayloadVisuals : byte
{
    Slots
}

[Flags]
[Serializable, NetSerializable]
public enum ChemicalPayloadFilledSlots : byte
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Both = Left | Right,
}
