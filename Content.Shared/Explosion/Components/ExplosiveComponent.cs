using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion.Components;

/// <summary>
///     Specifies an explosion that can be spawned by this entity. The explosion itself is spawned via <see
///     cref="ExplosionSystem.TriggerExplosive"/>.
/// </summary>
/// <remarks>
///      The total intensity may be overridden by whatever system actually calls TriggerExplosive(), but this
///      component still determines the explosion type and other properties.
/// </remarks>
[RegisterComponent, Access(typeof(SharedExplosionSystem))]
public sealed partial class ExplosiveComponent : Component
{
    /// <summary>
    ///     The explosion prototype. This determines the damage types, the tile-break chance, and some visual
    ///     information (e.g., the light that the explosion gives off).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ExplosionPrototype> ExplosionType = default!;

    /// <summary>
    ///     The maximum intensity the explosion can have on a single tile. This limits the maximum damage and tile
    ///     break chance the explosion can achieve at any given location.
    /// </summary>
    [DataField]
    public float MaxIntensity = 4;

    /// <summary>
    ///     How quickly the intensity drops off as you move away from the epicenter.
    /// </summary>
    [DataField]
    public float IntensitySlope = 1;

    /// <summary>
    ///     The total intensity of this explosion. The radius of the explosion scales like the cube root of this
    ///     number (see <see cref="ExplosionSystem.RadiusToIntensity"/>).
    /// </summary>
    /// <remarks>
    ///     This number can be overridden by passing optional argument to <see
    ///     cref="ExplosionSystem.TriggerExplosive"/>.
    /// </remarks>
    [DataField]
    public float TotalIntensity = 10;

    /// <summary>
    ///     Factor used to scale the explosion intensity when calculating tile break chances. Allows for stronger
    ///     explosives that don't space tiles, without having to create a new explosion-type prototype.
    /// </summary>
    [DataField]
    public float TileBreakScale = 1f;

    /// <summary>
    ///     Maximum number of times that an explosive can break a tile. Currently, for normal space stations breaking a
    ///     tile twice will generally result in a vacuum.
    /// </summary>
    [DataField]
    public int MaxTileBreak = int.MaxValue;

    /// <summary>
    ///     Whether this explosive should be able to create a vacuum by breaking tiles.
    /// </summary>
    [DataField]
    public bool CanCreateVacuum = true;

    /// <summary>
    /// An override for whether or not the entity should be deleted after it explodes.
    /// If null, the system calling the explode method handles it.
    /// </summary>
    [DataField]
    public bool? DeleteAfterExplosion;

    /// <summary>
    /// Whether to not set <see cref="Exploded"/> to true, allowing it to explode multiple times.
    /// This should never be used if it is damageable.
    /// </summary>
    [DataField]
    public bool Repeatable;

    /// <summary>
    ///     Avoid somehow double-triggering this explosion (e.g. by damaging this entity from its own explosion.
    /// </summary>
    public bool Exploded;
}
