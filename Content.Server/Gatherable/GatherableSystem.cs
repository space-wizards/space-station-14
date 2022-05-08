using System.Threading;
using Content.Server.Anprim14.Gathering.Components;
using Content.Server.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Anprim14.Gathering;

public sealed class GatherableSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly TagSystem _tagSystem = Get<TagSystem>();

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
            component.ToolWhitelist?.IsValid(args.Used) == false)
            return;

        if (tool.GatheringEntities.TryGetValue(uid, out var cancelToken))
        {
            cancelToken.Cancel();
            tool.GatheringEntities.Remove(uid);
            return;
        }

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

        _damageableSystem.TryChangeDamage(ev.Resource, tool.Damage);
        SoundSystem.Play(Filter.Pvs(ev.Resource, entityManager: EntityManager), tool.GatheringSound.GetSound(), ev.Resource);
        tool.GatheringEntities.Remove(ev.Resource);

        if (component.UseMappedLoot && component.MappedLoot != null)
        {
            foreach (var pair in component.MappedLoot)
            {
                if (!_tagSystem.HasTag(tool.Owner, pair.Key)) continue;
                var spawnLoot = EntitySpawnCollection.GetSpawns(pair.Value, _random);
                var playerPos = Transform(ev.Player).MapPosition;
                var spawnPos = playerPos.Offset(_random.NextVector2(0.3f));
                EntityManager.SpawnEntity(spawnLoot[0], spawnPos);
                tool.GatheringEntities.Remove(uid);
            }
        }
        else
        {
            if (component.Loot != null)
            {
                var spawnLoot = EntitySpawnCollection.GetSpawns(component.Loot, _random);
                var playerPos = Transform(ev.Player).MapPosition;
                var spawnPos = playerPos.Offset(_random.NextVector2(0.3f));
                EntityManager.SpawnEntity(spawnLoot[0], spawnPos);
            }

            tool.GatheringEntities.Remove(uid);
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



