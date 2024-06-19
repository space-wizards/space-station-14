namespace Content.Server.Geras;

[RegisterComponent]
public sealed partial class PersistantSlimeStorageComponent : Component
{
    [DataField]
    public TimeSpan InsertRemoveTime = TimeSpan.FromSeconds(3f); // Same as pockets by default,
}
