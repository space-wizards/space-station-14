using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.CartridgeLoader;

[RegisterComponent]
[ComponentReference(typeof(SharedCartridgeLoaderComponent))]
public sealed class CartridgeLoaderComponent : SharedCartridgeLoaderComponent
{
    /// <summary>
    /// The maximum amount of programs that can be installed on the cartridge loader entity
    /// </summary>
    [DataField("diskSpace")]
    public int DiskSpace = 5;

    [DataField("uiKey", readOnly: true, required: true, customTypeSerializer: typeof(EnumSerializer))]
    public Enum UiKey = default!;
}
