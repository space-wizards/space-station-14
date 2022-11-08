using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.CartridgeLoader;

[Access(typeof(SharedCartridgeLoaderSystem))]
public abstract class SharedCartridgeLoaderComponent : Component
{
    public const string CartridgeSlotId = "Cartridge-Slot";

    [DataField("cartridgeSlot")]
    public ItemSlot CartridgeSlot = new();

    /// <summary>
    /// List of programs that come preinstalled with this cartridge loader
    /// </summary>
    [DataField("preinstalled")]
    public List<string> PreinstalledPrograms = new();

    /// <summary>
    /// The currently running program that has its ui showing
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActiveProgram = default;

    /// <summary>
    /// The list of programs running in the background, listening to certain events
    /// </summary>
    [ViewVariables]
    public readonly List<EntityUid> BackgroundPrograms = new();

    /// <summary>
    /// The list of program entities that are spawned into the cartridge loaders program container
    /// </summary>
    [DataField("installedCartridges")]
    public List<EntityUid> InstalledPrograms = new();
}
