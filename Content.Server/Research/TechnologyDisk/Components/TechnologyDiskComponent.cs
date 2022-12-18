namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed class TechnologyDiskComponent : Component
{
    /// <summary>
    /// The recipe that will be added. If null,
    /// </summary>
    [DataField("recipe")]
    public string? Recipe;
}
