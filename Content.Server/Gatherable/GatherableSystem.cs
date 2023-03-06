using System.Threading;
using Content.Server.Destructible;
using Content.Server.DoAfter;
using Content.Server.Gatherable.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Destructible;
using Content.Shared.EntityList;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed class GatherableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GatherableComponent, DoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<GatheringToolComponent>(args.Used, out var tool) || component.ToolWhitelist?.IsValid(args.Used) == false)
            return;

        // Can't gather too many entities at once.
        if (tool.MaxGatheringEntities < tool.GatheringEntities.Count + 1)
            return;

        var damageRequired = _destructible.DestroyedAt(uid);
        var damageTime = (damageRequired / tool.Damage.Total).Float();
        damageTime = Math.Max(1f, damageTime);

        var doAfter = new DoAfterEventArgs(args.User, damageTime, target: uid, used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
        };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, GatherableComponent component, DoAfterEvent args)
    {
        if(!TryComp<GatheringToolComponent>(args.Args.Used, out var tool) || args.Args.Target == null)
            return;

        if (args.Handled || args.Cancelled)
        {
            tool.GatheringEntities.Remove(args.Args.Target.Value);
            return;
        }

        // Complete the gathering process
        _destructible.DestroyEntity(args.Args.Target.Value);
        _audio.PlayPvs(tool.GatheringSound, args.Args.Target.Value);
        tool.GatheringEntities.Remove(args.Args.Target.Value);

        // Spawn the loot!
        if (component.MappedLoot == null)
            return;

        var playerPos = Transform(args.Args.User).MapPosition;

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
        args.Handled = true;
    }
}



