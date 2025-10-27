namespace Content.Server.Mapping;

[RegisterComponent, UnsavedComponent]
public sealed partial class AutoSaveMapComponent : Component
{
    [DataField]
    public TimeSpan NextSaveTime;

    [DataField]
    public string FileName = string.Empty;
}
