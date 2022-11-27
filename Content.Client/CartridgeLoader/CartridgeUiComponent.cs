
namespace Content.Client.CartridgeLoader;

/// <summary>
/// The component used for defining which ui fragment to use for a cartridge
/// </summary>
/// <seealso cref="CartridgeUI"/>
/// <seealso cref="CartridgeUISerializer"/>
[RegisterComponent]
public sealed class CartridgeUiComponent : Component
{
    [DataField("ui", true)]
    public CartridgeUI? Ui = default;
}
