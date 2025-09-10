using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;

namespace Content.Shared.EntityEffects.NewEffects;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class CauseZombieInfectionEntityEffectsSystem : EntityEffectSystem<MobStateComponent, CauseZombieInfection>
{
    // MobState because you have to die to become a zombie...
    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CauseZombieInfection> args)
    {
        EnsureComp<ZombifyOnDeathComponent>(entity);
        EnsureComp<PendingZombieComponent>(entity);
    }
}

public sealed partial class CureZombieInfectionEntityEffectsSystem : EntityEffectSystem<MobStateComponent, CureZombieInfection>
{
    // MobState because you have to die to become a zombie...
    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CureZombieInfection> args)
    {
        // TODO: Server only...
        //if (HasComp<IncurableZombieComponent>(entity))
            //return;

        RemComp<ZombifyOnDeathComponent>(entity);
        RemComp<PendingZombieComponent>(entity);

        if (args.Effect.Innoculate)
            EnsureComp<ZombieImmuneComponent>(entity);

    }
}

public sealed partial class CauseZombieInfection : EntityEffectBase<CauseZombieInfection>;

public sealed partial class CureZombieInfection : EntityEffectBase<CureZombieInfection>
{
    /// <summary>
    /// Do we also protect against future infections?
    /// </summary>
    [DataField]
    public bool Innoculate;
}
