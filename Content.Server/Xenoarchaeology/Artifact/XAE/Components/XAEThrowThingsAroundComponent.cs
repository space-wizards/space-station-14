namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Throws all nearby entities backwards.
/// Also pries nearby tiles.
/// </summary>
[RegisterComponent, Access(typeof(XAEThrowThingsAroundSystem))]
public sealed partial class XAEThrowThingsAroundComponent : Component
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
