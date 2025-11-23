using Content.Shared.Damage.Components;
using Content.Shared.Explosion;
using Content.Shared.Gibbing.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Containers;

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
        SaveSpecialCauseOfDeath(ent, "Explosion");  //shouldnt be a string yeah make it a proto id
    }

    /// <summary>
    /// Gets an entity Mind and stores it's current body damages inside of it's LastBodyDamageComponent
    /// </summary>
    private void SaveBody(EntityUid ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable)
            || !TryComp<MindContainerComponent>(ent, out var mindContainer)
            || !TryComp<MindComponent>(mindContainer.Mind, out _))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);
        storedDamage.DamagePerGroup = damageable.DamagePerGroup;
        storedDamage.Damage = damageable.Damage;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }

    /// <summary>
    /// epic explanation
    /// </summary>
    private void SaveSpecialCauseOfDeath(EntityUid ent, string cause)  // shouldnt be a stringg thats bad u.u
    {
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)
            || !TryComp<MindComponent>(mindContainer.Mind, out _))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);

        storedDamage.SpecialCauseOfDeath = cause;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }

    /// <summary>
    /// also an explanation
    /// </summary>
    private void ClearSpecialCause(EntityUid ent)
    {
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)  // this should prolly be a method sincee im doing the same thingi 3 times tis system
            || !TryComp<MindComponent>(mindContainer.Mind, out _))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);

        storedDamage.SpecialCauseOfDeath = "none"; // temporary, again it should be a proto id not a string
        Dirty(mindContainer.Mind.Value, storedDamage);
    }
}
