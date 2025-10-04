using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// This is used for...
/// </summary>
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

public sealed partial class CureZombieInfectionEntityEffectsSystem : EntityEffectSystem<MobStateComponent, CureZombieInfection>
{
    // MobState because you have to die to become a zombie...
    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CureZombieInfection> args)
    {
        if (HasComp<IncurableZombieComponent>(entity))
            return;

        RemComp<ZombifyOnDeathComponent>(entity);
        RemComp<PendingZombieComponent>(entity);

        if (args.Effect.Innoculate)
            EnsureComp<ZombieImmuneComponent>(entity);
    }
}

public sealed partial class CauseZombieInfection : EntityEffectBase<CauseZombieInfection>
{
    protected override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-cause-zombie-infection", ("chance", Probability));
}

public sealed partial class CureZombieInfection : EntityEffectBase<CureZombieInfection>
{
    /// <summary>
    /// Do we also protect against future infections?
    /// </summary>
    [DataField]
    public bool Innoculate;

    protected override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Innoculate)
            return Loc.GetString("entity-effect-guidebook-innoculate-zombie-infection", ("chance", Probability));

        return Loc.GetString("entity-effect-guidebook-cure-zombie-infection", ("chance", Probability));
    }
}
