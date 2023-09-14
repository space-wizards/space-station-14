namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NotekeeperCartridgeComponent : Component
{
    /// <summary>
    /// The list of notes that got written down
    /// </summary>
    [DataField("notes")]
    public List<string> Notes = new();
}
