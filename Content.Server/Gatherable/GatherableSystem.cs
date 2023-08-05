using Content.Server.Destructible;
using Content.Server.Gatherable.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityList;
using Content.Shared.Gatherable;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GatherableComponent, GatherableDoAfterEvent>(OnDoAfter);
        InitializeProjectile();
    }

    private void Gather(EntityUid gatheredUid, EntityUid user, EntityUid used, GatheringToolComponent? tool = null, GatherableComponent? component = null)
    {
        if (!Resolve(used, ref tool, false) || !Resolve(gatheredUid, ref component, false) ||
            component.ToolWhitelist?.IsValid(used) == false)
        {
            return;
        }

        // Can't gather too many entities at once.
        if (tool.MaxGatheringEntities < tool.GatheringEntities.Count + 1)
            return;

        var damageRequired = _destructible.DestroyedAt(gatheredUid);
        var damageTime = (damageRequired / tool.Damage.Total).Float();
        damageTime = Math.Max(1f, damageTime);

        var doAfter = new DoAfterArgs(user, damageTime, new GatherableDoAfterEvent(), gatheredUid, target: gatheredUid, used: used)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnActivate(EntityUid uid, GatherableComponent component, ActivateInWorldEvent args)
    {
        Gather(uid, args.User, args.User);
    }

    private void OnInteractUsing(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        Gather(uid, args.User, args.Used, component: component);
    }

    private void OnDoAfter(EntityUid uid, GatherableComponent component, GatherableDoAfterEvent args)
    {
        if(!TryComp<GatheringToolComponent>(args.Args.Used, out var tool))
            return;

        tool.GatheringEntities.Remove(uid);
        if (args.Handled || args.Cancelled)
            return;

        Gather(uid, args.Args.Used, component, tool.GatheringSound);
        args.Handled = true;
    }

    public void Gather(EntityUid gatheredUid, EntityUid? gatherer = null, GatherableComponent? component = null, SoundSpecifier? sound = null)
    {
        if (!Resolve(gatheredUid, ref component))
            return;

        // Complete the gathering process
        _destructible.DestroyEntity(gatheredUid);
        _audio.PlayPvs(sound, gatheredUid);

        // Spawn the loot!
        if (component.MappedLoot == null)
            return;

        var pos = Transform(gatheredUid).MapPosition;

        foreach (var (tag, table) in component.MappedLoot)
        {
            if (tag != "All")
            {
                if (gatherer != null && !_tagSystem.HasTag(gatherer.Value, tag))
                    continue;
            }
            var getLoot = _prototypeManager.Index<EntityLootTablePrototype>(table);
            var spawnLoot = getLoot.GetSpawns();
            var spawnPos = pos.Offset(_random.NextVector2(0.3f));
            Spawn(spawnLoot[0], spawnPos);
        }
    }
}



