using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components;

/// <summary>
///     Specifies an explosion that can be spawned by this entity. The explosion itself is spawned via <see
///     cref="ExplosionSystem.TriggerExplosive"/>.
/// </summary>
/// <remarks>
///      The total intensity may be overridden by whatever system actually calls TriggerExplosive(), but this
///      component still determines the explosion type and other properties.
/// </remarks>
[RegisterComponent]
public sealed class ExplosiveComponent : Component
{
    public override string Name => "Explosive";

    /// <summary>
    ///     The explosion prototype. This determines the damage types, the tile-break chance, and some visual
    ///     information (e.g., the light that the explosion gives off).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("explosionType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ExplosionPrototype>))]
    public string ExplosionType = default!;

    /// <summary>
    ///     The maximum intensity the explosion can have on a single time. This limits the maximum damage and tile
    ///     break chance the explosion can achieve at any given location.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxIntensity")]
    public float MaxIntensity = 4;

    /// <summary>
    ///     How quickly the intensity drops off as you move away from the epicenter.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intensitySlope")]
    public float IntensitySlope = 1;

    /// <summary>
    ///     The total intensity of this explosion. The radius of the explosion scales like the cube root of this
    ///     number (see <see cref="ExplosionSystem.RadiusToIntensity"/>).
    /// </summary>
    /// <remarks>
    ///     This number can be overridden by passing optional argument to <see
    ///     cref="ExplosionSystem.TriggerExplosive"/>.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("totalIntensity")]
    public float TotalIntensity = 10;

    /// <summary>
    ///     Avoid somehow double-triggering this explosion (e.g. by damaging this entity from it's own explosion.
    /// </summary>
    public bool Exploded;
}
