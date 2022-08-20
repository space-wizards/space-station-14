using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.CartridgeLoader;

[Virtual]
[Access(typeof(SharedCartridgeLoaderSystem))]
public class SharedCartridgeLoaderComponent : Component
{
    public const string CartridgeSlotId = "Cartridge-Slot";

    [DataField("cartridgeSlot")]
    public ItemSlot CartridgeSlot = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActiveProgram = default;

    [ViewVariables]
    public readonly List<EntityUid> BackgroundPrograms = new();

    [DataField("installedCartridges")]
    public List<EntityUid> InstalledPrograms = new();
}
