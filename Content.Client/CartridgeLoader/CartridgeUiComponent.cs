
namespace Content.Client.CartridgeLoader;

[RegisterComponent]
public sealed class CartridgeUiComponent : Component
{
    [DataField("ui", true, customTypeSerializer: typeof(CartridgeUISerializer))]
    public CartridgeUI? Ui = default;
}
