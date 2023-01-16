using Content.Server.Actions;
using Content.Server.Animals.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

public sealed class EggLayerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggLayerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EggLayerComponent, EggLayInstantActionEvent>(OnEggLayAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var eggLayer in EntityQuery<EggLayerComponent>())
        {
            // Players should be using the action.
            if (HasComp<ActorComponent>(eggLayer.Owner))
                return;

            eggLayer.AccumulatedFrametime += frameTime;

            if (eggLayer.AccumulatedFrametime < eggLayer.CurrentEggLayCooldown)
                continue;

            eggLayer.AccumulatedFrametime -= eggLayer.CurrentEggLayCooldown;
            eggLayer.CurrentEggLayCooldown = _random.NextFloat(eggLayer.EggLayCooldownMin, eggLayer.EggLayCooldownMax);

            TryLayEgg(eggLayer.Owner, eggLayer);
        }
    }

    private void OnComponentInit(EntityUid uid, EggLayerComponent component, ComponentInit args)
    {
        if (!_prototype.TryIndex<InstantActionPrototype>(component.EggLayAction, out var action))
            return;

        _actions.AddAction(uid, new InstantAction(action), uid);
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

            hunger.CurrentHunger -= component.HungerUsage;
        }

        foreach (var ent in EntitySpawnCollection.GetSpawns(component.EggSpawn, _random))
        {
            Spawn(ent, Transform(uid).Coordinates);
        }

        // Sound + popups
        SoundSystem.Play(component.EggLaySound.GetSound(), Filter.Pvs(uid), uid, component.EggLaySound.Params);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-user"), uid, uid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-others", ("entity", uid)), uid, Filter.PvsExcept(uid), true);

        return true;
    }
}
