using Content.Server.Actions;
using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Animals;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Storage;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives the ability to lay eggs/other things;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
public sealed partial class EggLayerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HungerProductionSystem _hungerProduction = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, EggLayerComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, component.EggLayAction);
    }

    [SubscribeLocalEvent]
    private void OnEggLayAction(EntityUid uid, EggLayerComponent egglayer, EggLayInstantActionEvent args)
    {
        // Cooldown is handeled by ActionAnimalLayEgg in types.yml.
        args.Handled = _hungerProduction.TryProduce(uid, out var failure);
        if (failure == HungerProductionFailure.Hungry)
            _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), uid, uid);
    }

    [SubscribeLocalEvent]
    private void OnProduce(Entity<EggLayerComponent> ent, ref HungerProductionEvent args)
    {
        foreach (var spawn in EntitySpawnCollection.GetSpawns(ent.Comp.EggSpawn, _random))
        {
            SpawnNextToOrDrop(spawn, args.Owner);
        }

        _audio.PlayPvs(ent.Comp.EggLaySound, args.Owner);
        _popup.PopupEntity(
            Loc.GetString("action-popup-lay-egg-user"),
            Loc.GetString("action-popup-lay-egg-others", ("entity", Identity.Entity(args.Owner, EntityManager))),
            args.Owner,
            args.Owner);

        args.Produced = true;
    }
}
