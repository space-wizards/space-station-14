using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Gatherable.Components;
using Content.Shared.Damage;
using Content.Shared.EntityList;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed class GatherableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GatheringDoafterCancel>(OnDoafterCancel);
        SubscribeLocalEvent<GatherableComponent, GatheringDoafterSuccess>(OnDoafterSuccess);
    }

    private void OnInteractUsing(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<GatheringToolComponent>(args.Used, out var tool) ||
            component.ToolWhitelist?.IsValid(args.Used) == false ||
            tool.GatheringEntities.TryGetValue(uid, out var cancelToken))
            return;

        // Can't gather too many entities at once.
        if (tool.MaxGatheringEntities < tool.GatheringEntities.Count + 1)
            return;

        cancelToken = new CancellationTokenSource();
        tool.GatheringEntities[uid] = cancelToken;

        var doAfter = new DoAfterEventArgs(args.User, tool.GatheringTime, cancelToken.Token, uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
            BroadcastCancelledEvent = new GatheringDoafterCancel { Tool = args.Used, Resource = uid },
            TargetFinishedEvent = new GatheringDoafterSuccess { Tool = args.Used, Resource = uid, Player = args.User }
        };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterSuccess(EntityUid uid, GatherableComponent component, GatheringDoafterSuccess ev)
    {
        if (!TryComp(ev.Tool, out GatheringToolComponent? tool))
            return;

        // Complete the gathering process
        _damageableSystem.TryChangeDamage(ev.Resource, tool.Damage, origin: ev.Player);
        _audio.PlayPvs(tool.GatheringSound, ev.Resource);
        tool.GatheringEntities.Remove(ev.Resource);

        // Spawn the loot!
        if (component.MappedLoot == null)
            return;

        var playerPos = Transform(ev.Player).MapPosition;

        foreach (var (tag, table) in component.MappedLoot)
        {
            if (tag != "All")
            {
                if (!_tagSystem.HasTag(tool.Owner, tag))
                    continue;
            }
            var getLoot = _prototypeManager.Index<EntityLootTablePrototype>(table);
            var spawnLoot = getLoot.GetSpawns();
            var spawnPos = playerPos.Offset(_random.NextVector2(0.3f));
            Spawn(spawnLoot[0], spawnPos);
        }
    }

    private void OnDoafterCancel(GatheringDoafterCancel ev)
    {
        if (!TryComp<GatheringToolComponent>(ev.Tool, out var tool))
            return;

        tool.GatheringEntities.Remove(ev.Resource);
    }

    private sealed class GatheringDoafterCancel : EntityEventArgs
    {
        public EntityUid Tool;
        public EntityUid Resource;
    }

    private sealed class GatheringDoafterSuccess : EntityEventArgs
    {
        public EntityUid Tool;
        public EntityUid Resource;
        public EntityUid Player;
    }
}



