using Content.Server.Actions;
using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Actions.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Robust.Server.Audio;
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

        // Allow infinitely laying eggs if they can't get hungry
        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            if (hunger.CurrentHunger < component.HungerUsage)
            {
                _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), uid, uid);
                return false;
            }

            _hunger.ModifyHunger(uid, -component.HungerUsage, hunger);
        }

        foreach (var ent in EntitySpawnCollection.GetSpawns(component.EggSpawn, _random))
        {
            Spawn(ent, Transform(uid).Coordinates);
        }

        // Sound + popups
        _audio.PlayPvs(component.EggLaySound, uid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-user"), uid, uid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-others", ("entity", uid)), uid, Filter.PvsExcept(uid), true);

        return true;
    }
}
