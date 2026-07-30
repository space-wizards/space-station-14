using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes the zombie infection on this entity.
/// </summary>
/// <remarks>
/// This is on the server because <see cref="PendingZombieComponent"/> is only used on the server and has session-specific networking
/// to prevent cheaters from seeing initial infected.
/// </remarks>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class CauseZombieInfectionEntityEffectsSystem : EntityEffectSystem<MobStateComponent, CauseZombieInfection>
{
    // MobState because you have to die to become a zombie...
    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CauseZombieInfection> args)
    {
        if (HasComp<ZombieImmuneComponent>(entity) || HasComp<IncurableZombieComponent>(entity))
            return;

        EnsureComp<ZombifyOnDeathComponent>(entity);
        EnsureComp<PendingZombieComponent>(entity);
    }
}
