namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class NewsReadCartridgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int ArticleNum;
}
