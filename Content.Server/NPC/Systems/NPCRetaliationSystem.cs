using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
///     Handles NPC which become aggressive after being attacked.
/// </summary>
public sealed class NPCRetaliationSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<NPCRetaliationComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<NPCRetaliationComponent, DisarmedEvent>(OnDisarmed);
    }

    private void OnDamageChanged(EntityUid uid, NPCRetaliationComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.Origin is not { } origin)
            return;

        TryRetaliate(uid, origin, component);
    }

    private void OnDisarmed(EntityUid uid, NPCRetaliationComponent component, DisarmedEvent args)
    {
        TryRetaliate(uid, args.Source, component);
    }

    public bool TryRetaliate(EntityUid uid, EntityUid target, NPCRetaliationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // don't retaliate against inanimate objects.
        if (!HasComp<MobStateComponent>(target))
            return false;

        if (_npcFaction.IsEntityFriendly(uid, target))
            return false;

        _npcFaction.AggroEntity(uid, target);
        if (component.AttackMemoryLength is { } memoryLength)
            component.AttackMemories[target] = _timing.CurTime + memoryLength;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCRetaliationComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var retaliationComponent, out var factionException))
        {
            foreach (var entity in new ValueList<EntityUid>(retaliationComponent.AttackMemories.Keys))
            {
                if (!TerminatingOrDeleted(entity) && _timing.CurTime < retaliationComponent.AttackMemories[entity])
                    continue;

                _npcFaction.DeAggroEntity(uid, entity, factionException);
            }
        }
    }
}
