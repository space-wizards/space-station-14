namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class NotekeeperCartridgeComponent : Component
{
    [DataField("notes")]
    public List<string> Notes = new();
}
