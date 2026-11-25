using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CartridgeLoaderSystem))]
public sealed partial class CartridgeLoaderComponent : Component
{
    public const string UnremovableContainerId = "preinstalled-program-container";

    public const string RemovableContainerId = "removable-program-container";

    public const string CartridgeSlotId = "Cartridge-Slot";

    [DataField]
    public ItemSlot CartridgeSlot = new();

    /// <summary>
    /// The currently running program that has its ui showing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveProgram = default;

    /// <summary>
    /// The maximum amount of programs that can be installed on the cartridge loader entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DiskSpace = 8;

    /// <summary>
    /// Controls whether the cartridge loader will play notifications if it supports it at all
    /// TODO: Add an option for this to the PDA
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NotificationsEnabled = true;

    [DataField(required: true)]
    public Enum UiKey = default!;
}
