namespace Content.Shared.Cuffs.Components;
public abstract class SharedLegCuffableComponent : Component
{
    /// <summary>
    /// How slow should the legcuffed entity be?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("slowdown")]
    public float Slowdown = 0.3f;

    /// <summary>
    /// Is this entity currently legcuffed?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isCuffed")]
    public bool IsCuffed;
}
