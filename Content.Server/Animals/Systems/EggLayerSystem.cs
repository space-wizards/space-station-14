using Content.Server.Actions;
using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives the ability to lay eggs/other things;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
public sealed partial class EggLayerSystem : EntitySystem
{
    private static readonly EntityTimerId GrowthTimer = new("growth");

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggLayerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EggLayerComponent, EggLayInstantActionEvent>(OnEggLayAction);
        SubscribeLocalEvent<EggLayerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(Entity<EggLayerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != GrowthTimer)
            return;

        ent.Comp.NextGrowth = args.ScheduledTime + TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.EggLayCooldownMin, ent.Comp.EggLayCooldownMax));
        _timers.SetTimerAt(ent, GrowthTimer, ent.Comp.NextGrowth);

        if (!HasComp<ActorComponent>(ent) && !_mobState.IsDead(ent))
            TryLayEgg(ent, ent.Comp);
    }

    private void OnMapInit(EntityUid uid, EggLayerComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, component.EggLayAction);
        component.NextGrowth = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.EggLayCooldownMin, component.EggLayCooldownMax));
        _timers.SetTimerAt<EggLayerComponent>((uid, component), GrowthTimer, component.NextGrowth);
    }

    private void OnEggLayAction(EntityUid uid, EggLayerComponent egglayer, EggLayInstantActionEvent args)
    {
        // Cooldown is handeled by ActionAnimalLayEgg in types.yml.
        args.Handled = TryLayEgg(uid, egglayer);
    }

    public bool TryLayEgg(EntityUid uid, EggLayerComponent? egglayer)
    {
        if (!Resolve(uid, ref egglayer))
            return false;

        if (_mobState.IsDead(uid))
            return false;

        // Allow infinitely laying eggs if they can't get hungry.
        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            if (_hunger.GetHunger(hunger) < egglayer.HungerUsage)
            {
                _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), uid, uid);
                return false;
            }

            _hunger.ModifyHunger(uid, -egglayer.HungerUsage, hunger);
        }

        foreach (var ent in EntitySpawnCollection.GetSpawns(egglayer.EggSpawn, _random))
        {
            Spawn(ent, Transform(uid).Coordinates);
        }

        // Sound + popups
        _audio.PlayPvs(egglayer.EggLaySound, uid);
        _popup.PopupEntity(
            Loc.GetString("action-popup-lay-egg-user"),
            Loc.GetString("action-popup-lay-egg-others", ("entity", Identity.Entity(uid, EntityManager))),
            uid,
            uid);

        return true;
    }
}
