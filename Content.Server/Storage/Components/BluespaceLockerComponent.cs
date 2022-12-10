namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed class BluespaceLockerComponent : Component
{
    /// <summary>
    /// If length > 0, when something is added to the storage, it will instead be teleported to a random storage
    /// from the list and the other storage will be opened.
    /// </summary>
    [DataField("bluespaceLinks"), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityStorageComponent> BluespaceLinks = new();
}
