namespace Content.Client.CartridgeComputer;

public sealed class CartridgeUIComponent : Component
{
    [DataField("ui", true, customTypeSerializer: typeof(CartridgeUISerializer))]
    public CartridgeUI? Ui = default;
}
