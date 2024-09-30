using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.ImmovableRod;

[RegisterComponent]
public sealed partial class ImmovableRodComponent : Component
{
    public int MobCount = 0;

    [DataField("hitSound")]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("MetalSlam");

    [DataField("hitSoundProbability")]
    public float HitSoundProbability = 0.1f;

    [DataField("minSpeed")]
    public float MinSpeed = 10f;

    [DataField("maxSpeed")]
    public float MaxSpeed = 35f;

    /// <remarks>
    ///     Stuff like wizard rods might want to set this to false, so that they can set the velocity themselves.
    /// </remarks>
    [DataField("randomizeVelocity")]
    public bool RandomizeVelocity = true;

    /// <summary>
    ///     Overrides the random direction for an immovable rod.
    /// </summary>
    [DataField("directionOverride")]
    public Angle DirectionOverride = Angle.Zero;

    /// <summary>
    ///     With this set to true, rods will automatically set the tiles under them to space.
    /// </summary>
    [DataField("destroyTiles")]
    public bool DestroyTiles = true;

    /// <summary>
    ///     If true, this will gib & delete bodies
    /// </summary>
    [DataField]
    public bool ShouldGib = true;

    /// <summary>
    ///     Damage done, if not gibbing
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;
}
