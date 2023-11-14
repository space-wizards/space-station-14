using Content.Server.Actions;
using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Actions.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

public sealed class EggLayerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggLayerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EggLayerComponent, EggLayInstantActionEvent>(OnEggLayAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EggLayerComponent>();
        while (query.MoveNext(out var uid, out var eggLayer))
        {
            // Players should be using the action.
            if (HasComp<ActorComponent>(uid))
                continue;

            eggLayer.AccumulatedFrametime += frameTime;

            if (eggLayer.AccumulatedFrametime < eggLayer.CurrentEggLayCooldown)
                continue;

            eggLayer.AccumulatedFrametime -= eggLayer.CurrentEggLayCooldown;
            eggLayer.CurrentEggLayCooldown = _random.NextFloat(eggLayer.EggLayCooldownMin, eggLayer.EggLayCooldownMax);

            TryLayEgg(uid, eggLayer);
        }
    }

    private void OnMapInit(EntityUid uid, EggLayerComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, component.EggLayAction);
        component.CurrentEggLayCooldown = _random.NextFloat(component.EggLayCooldownMin, component.EggLayCooldownMax);
    }

    private void OnEggLayAction(EntityUid uid, EggLayerComponent component, EggLayInstantActionEvent args)
    {
        args.Handled = TryLayEgg(uid, component);
    }

    public bool TryLayEgg(EntityUid uid, EggLayerComponent? component)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_mobState.IsAlive(uid))
            return false;

        // check hungry value
        if (!TryComp<HungerComponent>(uid, out var hunger)
            || hunger.CurrentHunger < component.HungerUsage)
        {
            _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), uid, uid);
            return false;
        }

        foreach (var protoEntId in EntitySpawnCollection.GetSpawns(component.EggSpawn, _random))
        {
            // check for ability spawn many items for one tile
            if (component.ManySpawnsForbidden)
            {
                var entitiesInTile = _lookup.GetEntitiesIntersecting(uid, LookupFlags.All);

                foreach (var tileEntyUid in entitiesInTile)
                {
                    var metaDataComp = _entManager.GetComponent<MetaDataComponent>(tileEntyUid);

                    if (metaDataComp?.EntityPrototype?.ID == protoEntId)
                    {
                        return false;
                    }
                }
            }

            _hunger.ModifyHunger(uid, -component.HungerUsage, hunger);
            var spawnedUid = Spawn(protoEntId, Transform(uid).Coordinates);
            ShowSpawnPopus(uid, spawnedUid, component);
        }

        return true;
    }

    void ShowSpawnPopus(EntityUid spawnerUid, EntityUid newItemUid, EggLayerComponent comp)
    {
        // Sound + popups
        _audio.PlayPvs(comp.EggLaySound, spawnerUid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-user", ("newItem", newItemUid)), spawnerUid, spawnerUid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-others", ("spawner", spawnerUid), ("newItem", newItemUid)),
            spawnerUid, Filter.PvsExcept(spawnerUid), true);
    }
}
