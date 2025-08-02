using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ImmovableRod;

/// <summary>
/// Marker for entities that represent unstoppable object,
/// that crushes through obstacles and mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImmovableRodComponent : Component
{
    /// <summary>
    /// Tracks the number of mobs that this rod has slaughtered.
    /// </summary>
    public int MobCount = 0;

    /// <summary>
    /// How should this rod CLANG?
    /// </summary>
    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("MetalSlam");

    /// <summary>
    /// How often should this rod CLANG?
    /// </summary>
    [DataField]
    public float HitSoundProbability = 0.1f;

    /// <summary>
    /// How slow can this rod be going?
    /// </summary>
    [DataField]
    public float MinSpeed = 10f;

    /// <summary>
    /// How fast can this rod be going?
    /// </summary>
    [DataField]
    public float MaxSpeed = 35f;

    /// <summary>
    /// Should the rod randomize its velocity on spawn?
    /// </summary>
    /// <remarks>
    /// Should be false for entity-controlled mobs, such as polymorphed wizards.
    /// </remarks>
    [DataField]
    public bool RandomizeVelocity = true;

    /// <summary>
    /// Overrides the random direction for an immovable rod.
    /// </summary>
    [DataField]
    public Angle DirectionOverride = Angle.Zero;

    /// <summary>
    /// How much damage, if any, does this rod do?
    /// </summary>
    /// <remarks>
    /// Remember that structural damage is there to allow for mega-damage to inorganic matter without obliterating
    /// Urist McBystander!
    /// </remarks>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// How much stamina damage should this rod do to appropriate mobs?
    /// </summary>
    [DataField]
    public float StaminaDamage = 200.0f;

    /// <summary>
    /// Should this rod spawn anything when it hits another rod?
    /// </summary>
    [DataField]
    public EntProtoId? SpawnOnRodCollision = "Singularity";

    /// <summary>
    /// If this rod spawns anything when it hits another rod, what popup text should appear?
    /// </summary>
    [DataField]
    public LocId OnRodCollisionPopup = "immovable-rod-collided-rod-not-good";

    /// <summary>
    /// What popup should be displayed when this rod hits someone?
    /// </summary>
    [DataField]
    public LocId OnMobCollisionPopup = "immovable-rod-penetrated-mob"; // Ma'am, this is a Christian Minecraft server.

    /// <summary>
    /// What fixture is used to detect that the rod has hit a mob?
    /// </summary>
    [DataField]
    public string MobCollisionFixtureId = "flammable"; // Temporary default; aligns with mob collision code.

    /// <summary>
    /// Has this rod already hit another immovable rod? Used to avoid "oops, two singularities!"
    /// bugs because collisions are evaluated on the same simulation tick.
    /// </summary>
    public bool HasCollidedWithRod;
}
