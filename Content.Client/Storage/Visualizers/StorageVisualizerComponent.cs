namespace Content.Client.Storage.Visualizers;

[RegisterComponent]
[Access(typeof(StorageVisualizerSystem))]
public sealed class EntityStorageVisualsComponent : Component
{
    [DataField("state")]
    public string? StateBase;
    [DataField("state_alt")]
    public string? StateBaseAlt;
    [DataField("state_open")]
    public string? StateOpen;
    [DataField("state_closed")]
    public string? StateClosed;
}
