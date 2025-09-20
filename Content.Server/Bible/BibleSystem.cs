using Content.Server.Bible.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Actions;
using Content.Shared.Bible;
using Content.Shared.Bible.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.Bible;

public sealed class BibleSystem : SharedBibleSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SummonableComponent, GetItemActionsEvent>(GetSummonAction);
        SubscribeLocalEvent<SummonableComponent, SummonActionEvent>(OnSummon);
        SubscribeLocalEvent<FamiliarComponent, MobStateChangedEvent>(OnFamiliarDeath);
        SubscribeLocalEvent<FamiliarComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
    }

    private readonly Queue<EntityUid> _addQueue = new();
    private readonly Queue<EntityUid> _remQueue = new();

    /// <summary>
    /// This handles familiar respawning.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var entity in _addQueue)
        {
            EnsureComp<SummonableRespawningComponent>(entity);
        }
        _addQueue.Clear();

        foreach (var entity in _remQueue)
        {
            RemComp<SummonableRespawningComponent>(entity);
        }
        _remQueue.Clear();

        var query = EntityQueryEnumerator<SummonableRespawningComponent, SummonableComponent>();
        while (query.MoveNext(out var uid, out var _, out var summonableComp))
        {
            if (_timing.CurTime < summonableComp.RespawnEndTime)
                continue;

            // Clean up the old body
            if (summonableComp.Summon != null)
            {
                Del(summonableComp.Summon.Value);
                summonableComp.Summon = null;
            }

            summonableComp.AlreadySummoned = false;
            PopupSys.PopupEntity(Loc.GetString("bible-summon-respawn-ready", ("book", uid)), uid, PopupType.Medium);
            AudioSys.PlayPvs(summonableComp.SummonSound, uid);
            // Clean up the respawnEndTime and respawn tracking component
            summonableComp.RespawnEndTime = _timing.CurTime + summonableComp.RespawnInterval;
            _remQueue.Enqueue(uid);
            Dirty(uid, summonableComp);
        }
    }



    private void GetSummonAction(Entity<SummonableComponent> ent, ref GetItemActionsEvent args)
    {
        var component = ent.Comp;

        if (component.AlreadySummoned)
            return;

        args.AddAction(ref component.SummonActionEntity, component.SummonAction);
    }

    private void OnSummon(Entity<SummonableComponent> ent, ref SummonActionEvent args)
    {
        AttemptSummon(ent, args.Performer);
    }

    /// <summary>
    /// Starts up the respawn stuff when
    /// the chaplain's familiar dies.
    /// </summary>
    private void OnFamiliarDeath(Entity<FamiliarComponent> ent, ref MobStateChangedEvent args)
    {
        var component = ent.Comp;

        if (args.NewMobState != MobState.Dead || component.Source == null)
            return;

        var source = component.Source;
        if (source != null && HasComp<SummonableComponent>(source))
        {
            _addQueue.Enqueue(source.Value);
        }
    }

    /// <summary>
    /// When the familiar spawns, set its source to the bible.
    /// </summary>
    private void OnSpawned(Entity<FamiliarComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        var parent = Transform(args.Spawner).ParentUid;
        if (!TryComp<SummonableComponent>(parent, out var summonable))
            return;

        ent.Comp.Source = parent;
        summonable.Summon = ent.Owner;
    }

    protected override void Summon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent position)
    {
        var component = ent.Comp;

        // Make this familiar the component's summon
        var familiar = Spawn(component.SpecialItemPrototype, position.Coordinates);
        component.Summon = familiar;

        // If this is going to use a ghost role mob spawner, attach it to the bible.
        if (HasComp<GhostRoleMobSpawnerComponent>(familiar))
        {
            PopupSys.PopupEntity(Loc.GetString("bible-summon-requested"), user, user, PopupType.Medium);
            _transform.SetParent(familiar, ent.Owner);
        }
        component.AlreadySummoned = true;
        _actionsSystem.RemoveAction(user, component.SummonActionEntity);
        Dirty(ent);
    }
}
