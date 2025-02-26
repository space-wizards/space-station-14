using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.ImmovableRod;

[RegisterComponent]
[AutoGenerateComponentPause]
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

    /// <summary>
    ///     The string used when the rod gibs a mob.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId EviscerationPopup = "immovable-rod-penetrated-mob";

    /// <summary>
    ///     The time between gib popups
    /// </summary>
    public TimeSpan EviscerationPopupDelay = TimeSpan.FromMilliseconds(200);

    [AutoPausedField]
    public TimeSpan NextEviscerationPopup = new();
}
