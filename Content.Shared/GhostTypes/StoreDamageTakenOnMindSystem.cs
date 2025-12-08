using Content.Shared.Damage.Components;
using Content.Shared.Explosion;
using Content.Shared.Gibbing.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.GhostTypes;

public sealed class StoreDamageTakenOnMindSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, AttemptEntityGibEvent>(SaveBodyOnGib);
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, MobStateChangedEvent>(SaveBodyOnThreshold);
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, BeforeExplodeEvent>(DeathByExplosion);
    }

    /// <summary>
    /// Saves the damage of a player body inside their MindComponent after an attempted gib event
    /// </summary>
    private void SaveBodyOnGib(Entity<StoreDamageTakenOnMindComponent> ent, ref AttemptEntityGibEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        SaveBody(container.Owner);
    }

    /// <summary>
    /// Saves the damage of a player body inside their MindComponent after a damage threshold event
    /// </summary>
    private void SaveBodyOnThreshold(Entity<StoreDamageTakenOnMindComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            ClearSpecialCause(ent);

        SaveBody(ent.Owner);
    }

    private void DeathByExplosion(Entity<StoreDamageTakenOnMindComponent> ent, ref BeforeExplodeEvent args)
    {
        ProtoId<SpecialCauseOfDeathPrototype> casePrototype = "Explosion";
        SaveSpecialCauseOfDeath(ent, casePrototype);
    }

    /// <summary>
    /// Gets an entity Mind and stores it's current body damages inside of it's LastBodyDamageComponent
    /// </summary>
    private void SaveBody(EntityUid ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable)
            || !TryComp<MindContainerComponent>(ent, out var mindContainer)
            || !HasComp<MindComponent>(mindContainer.Mind))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);
        storedDamage.DamagePerGroup = damageable.DamagePerGroup;
        storedDamage.Damage = damageable.Damage;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }

    /// <summary>
    /// Saves an specific cause of death inside of an entity LastBodyDamageComponent
    /// </summary>
    private void SaveSpecialCauseOfDeath(EntityUid ent, ProtoId<SpecialCauseOfDeathPrototype> cause)
    {
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)
            || !HasComp<MindComponent>(mindContainer.Mind))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);

        storedDamage.SpecialCauseOfDeath = cause;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }

    /// <summary>
    /// Clears the specific cause of death of an entity LastBodyDamageComponent
    /// </summary>
    private void ClearSpecialCause(EntityUid ent)
    {
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)  // this should prolly be a method sincee im doing the same thingi 3 times tis system
            || !HasComp<MindComponent>(mindContainer.Mind))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);

        storedDamage.SpecialCauseOfDeath = null;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }
}
