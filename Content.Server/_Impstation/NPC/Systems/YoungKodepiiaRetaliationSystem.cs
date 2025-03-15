using Content.Server._Impstation.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Hands;
using Content.Shared.Pointing;

namespace Content.Server._Impstation.NPC.Systems;

/// <summary>
///     Handles NPC which become aggressive after being interacted with.
///     Modified from NPCRetaliationSystem
/// </summary>
public sealed class YoungKodepiiaRetaliationSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, PullStartedMessage>(OnPull);
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, AttackedEvent>(OnAttack);
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, GotEquippedHandEvent>(OnPickup);
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, AfterGotPointedAtEvent>(OnPointedAt);
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, ActivateInWorldEvent>(OnAfterInteract); //TODO: comment out once item support exists
    }

    private void OnPull(Entity<YoungKodepiiaRetaliationComponent> ent, ref PullStartedMessage args)
    {
        TryRetaliate(ent, args.PullerUid);
    }

    private void OnAttack(Entity<YoungKodepiiaRetaliationComponent> ent, ref AttackedEvent args)
    {
        TryRetaliate(ent, args.User);
    }

    private void OnPickup(Entity<YoungKodepiiaRetaliationComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = TryRetaliate(ent, args.User);
    }

    private void OnPointedAt(Entity<YoungKodepiiaRetaliationComponent> ent, ref AfterGotPointedAtEvent args)
    {
        TryRetaliate(ent, args.Pointer);
    }

    private void OnAfterInteract(Entity<YoungKodepiiaRetaliationComponent> ent, ref ActivateInWorldEvent args)
    {
        TryRetaliate(ent, args.User);
    }

    public bool TryRetaliate(Entity<YoungKodepiiaRetaliationComponent> ent, EntityUid target)
    {
        // don't retaliate against inanimate objects.
        if (!HasComp<MobStateComponent>(target))
            return false;

        // don't retaliate against the same faction
        if (_npcFaction.IsEntityFriendly(ent.Owner, target))
            return false;

        _npcFaction.AggroEntity(ent.Owner, target);
        if (ent.Comp.AttackMemoryLength is {} memoryLength)
            ent.Comp.AttackMemories[target] = _timing.CurTime + memoryLength;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<YoungKodepiiaRetaliationComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var retaliationComponent, out var factionException))
        {
            // TODO: can probably reuse this allocation and clear it
            foreach (var entity in new ValueList<EntityUid>(retaliationComponent.AttackMemories.Keys))
            {
                if (!TerminatingOrDeleted(entity) && _timing.CurTime < retaliationComponent.AttackMemories[entity])
                    continue;

                _npcFaction.DeAggroEntity((uid, factionException), entity);
                // TODO: should probably remove the AttackMemory, thats the whole point of the ValueList right??
            }
        }
    }
}
