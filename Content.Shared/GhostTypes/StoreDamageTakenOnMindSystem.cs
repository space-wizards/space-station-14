using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.GhostTypes;

public sealed class StoreDamageTakenOnMindSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, DestructionEventArgs>(SaveBodyOnGib);
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, MobStateChangedEvent>(SaveBodyOnThreshold);
        SubscribeLocalEvent<StoreDamageTakenOnMindComponent, BeforeExplodeEvent>(DeathByExplosion);
    }

    /// <summary>
    /// Saves the damage of a player body inside their MindComponent after a DestructionEventArgs
    /// </summary>
    private void SaveBodyOnGib(Entity<StoreDamageTakenOnMindComponent> ent, ref DestructionEventArgs args)
    {
        SaveBody(ent.Owner);
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
        SaveSpecialCauseOfDeath(ent, "Explosion");
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

        var protoDict = new Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>();
        foreach (var stringDict in damageable.DamagePerGroup)  // Translates the strings into ProtoId's before saving the Dictionary
        {
            if (!_proto.TryIndex(stringDict.Key, out DamageGroupPrototype? proto))
                continue;
            protoDict.TryAdd(proto, stringDict.Value);
        }

        storedDamage.DamagePerGroup = protoDict;
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
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer)
            || !HasComp<MindComponent>(mindContainer.Mind))
            return;

        EnsureComp<LastBodyDamageComponent>(mindContainer.Mind.Value, out var storedDamage);

        storedDamage.SpecialCauseOfDeath = null;
        Dirty(mindContainer.Mind.Value, storedDamage);
    }
}
