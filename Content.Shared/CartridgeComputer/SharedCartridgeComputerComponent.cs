using System.Collections.Immutable;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.CartridgeComputer;

[Access(typeof(SharedCartridgeComputerSystem))]
public abstract class SharedCartridgeComputerComponent : Component
{
    public const string CartridgeSlotId = "Cartridge-Slot";

    [DataField("cartridgeSlot")]
    public ItemSlot CartridgeSlot = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActiveProgram = default;

    [ViewVariables]
    public readonly List<EntityUid> BackgroundPrograms = new();
}
