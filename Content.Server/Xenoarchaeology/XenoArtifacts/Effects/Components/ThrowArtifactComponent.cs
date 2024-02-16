namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Throws all nearby entities backwards.
/// Also pries nearby tiles.
/// </summary>
[RegisterComponent]
public sealed partial class ThrowArtifactComponent : Component
{
    /// <summary>
    /// How close do you have to be to get yeeted?
    /// </summary>
    [DataField("range")]
    public float Range = 2f;

    /// <summary>
    /// How likely is it that an individual tile will get pried?
    /// </summary>
    [DataField("tilePryChance")]
    public float TilePryChance = 0.5f;

    /// <summary>
    /// How strongly does stuff get thrown?
    /// </summary>
    [DataField("throwStrength"), ViewVariables(VVAccess.ReadWrite)]
    public float ThrowStrength = 5f;
}
