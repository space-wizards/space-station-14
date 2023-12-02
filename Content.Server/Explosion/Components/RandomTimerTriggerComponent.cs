using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Explosion.Components;

/// <summary>
/// This is used for randomizing a <see cref="RandomTimerTriggerComponent"/> on MapInit
/// </summary>
[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed partial class RandomTimerTriggerComponent : Component
{
    /// <summary>
    /// The minimum random trigger time.
    /// </summary>
    [DataField]
    public float Min;

    /// <summary>
    /// The maximum random trigger time.
    /// </summary>
    [DataField]
    public float Max;
}
