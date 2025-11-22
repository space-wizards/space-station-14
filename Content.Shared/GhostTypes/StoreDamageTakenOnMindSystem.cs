using Content.Shared.Damage.Components;
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
        SaveBody(ent.Owner);
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
        Dirty(mindContainer.Mind.Value, storedDamage);
        storedDamage.DamagePerGroup = damageable.DamagePerGroup;
        storedDamage.Damage = damageable.Damage;
    }
}
