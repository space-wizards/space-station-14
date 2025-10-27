namespace Content.Server.Mapping;

[RegisterComponent, UnsavedComponent]
public sealed partial class AutoSaveComponent : Component
{
    [DataField]
    public TimeSpan NextSaveTime;

    [DataField]
    public string FileName = string.Empty;
}
