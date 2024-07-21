using Content.Shared.Explosion.Components;

namespace Content.Shared.Explosion.EntitySystems;

/// <summary>
/// Lets code in shared trigger explosions.
/// All processing is still done clientside.
/// </summary>
public abstract class SharedExplosionSystem : EntitySystem
{
    /// <summary>
    ///     Given an entity with an explosive component, spawn the appropriate explosion.
    /// </summary>
    /// <remarks>
    ///     Also accepts radius or intensity arguments. This is useful for explosives where the intensity is not
    ///     specified in the yaml / by the component, but determined dynamically (e.g., by the quantity of a
    ///     solution in a reaction).
    /// </remarks>
    public virtual void TriggerExplosive(EntityUid uid, ExplosiveComponent? explosive = null, bool delete = true, float? totalIntensity = null, float? radius = null, EntityUid? user = null)
    {
    }
}
