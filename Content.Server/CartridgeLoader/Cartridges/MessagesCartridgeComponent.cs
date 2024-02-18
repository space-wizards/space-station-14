namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessagesCartridgeComponent : Component
{
    /// <summary>
    /// The list of notes that got written down
    /// </summary>
    [DataField("notes")]
    public List<string> Notes = new();

    /// <summary>
    /// the uid of the current user
    /// </summary>
    public int? userUid = null;

}
