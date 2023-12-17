using Content.Server.Destructible;
using Content.Server.Gatherable.Components;
using Content.Shared.EntityList;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GatherableComponent, AttackedEvent>(OnAttacked);
        InitializeProjectile();
    }

    private void OnAttacked(EntityUid uid, GatherableComponent component, AttackedEvent args)
    {
        if (component.ToolWhitelist?.IsValid(args.Used, EntityManager) != true)
            return;

        Gather(uid, args.User, component);
    }

    private void OnActivate(EntityUid uid, GatherableComponent component, ActivateInWorldEvent args)
    {
        if (component.ToolWhitelist?.IsValid(args.User, EntityManager) != true)
            return;

        Gather(uid, args.User, component);
    }

    public void Gather(EntityUid gatheredUid, EntityUid? gatherer = null, GatherableComponent? component = null)
    {
        if (!Resolve(gatheredUid, ref component))
            return;

        if (TryComp<SoundOnGatherComponent>(gatheredUid, out var soundComp))
        {
            _audio.PlayPvs(soundComp.Sound, Transform(gatheredUid).Coordinates);
        }

        // Complete the gathering process
        _destructible.DestroyEntity(gatheredUid);

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
            var spawnLoot = getLoot.GetSpawns(_random);
            var spawnPos = pos.Offset(_random.NextVector2(0.3f));
            Spawn(spawnLoot[0], spawnPos);
        }
    }
}



