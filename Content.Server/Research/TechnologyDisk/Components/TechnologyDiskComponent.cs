namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed class TechnologyDiskComponent : Component
{
    /// <summary>
    /// The recipe that will be added. If null, one will be randomly generated
    /// </summary>
    [DataField("recipes")]
    public List<string>? Recipes;
}
