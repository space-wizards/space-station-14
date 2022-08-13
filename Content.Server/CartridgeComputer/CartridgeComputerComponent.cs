using Content.Shared.CartridgeComputer;

namespace Content.Server.CartridgeComputer;

[RegisterComponent]
[ComponentReference(typeof(SharedCartridgeComputerComponent))]
public sealed class CartridgeComputerComponent : SharedCartridgeComputerComponent
{
    [DataField("diskSpace")]
    public int DiskSpace = 5;

    [DataField("installedCartridges")]
    public List<EntityUid> InstalledPrograms = new();
}
