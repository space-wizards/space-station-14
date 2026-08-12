using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Robust.Server.Audio;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles the egg-laying action and feedback after eggs are produced.
/// </summary>
public sealed partial class EggLayerSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HungerProductionSystem _hungerProduction = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnEggLayAction(Entity<EggLayerComponent> ent, ref EggLayInstantActionEvent args)
    {
        // Cooldown is handled by ActionAnimalLayEgg in types.yml.
        args.Handled = _hungerProduction.TryProduce(ent.Owner, out var failure);
        if (failure == HungerProductionFailure.Hungry)
            _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), ent.Owner, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEntitiesProduced(Entity<EggLayerComponent> ent, ref EntitiesProducedEvent args)
    {
        _audio.PlayPvs(ent.Comp.EggLaySound, args.Owner);
        _popup.PopupEntity(
            Loc.GetString("action-popup-lay-egg-user"),
            Loc.GetString("action-popup-lay-egg-others", ("entity", Identity.Entity(args.Owner, EntityManager))),
            args.Owner,
            args.Owner);
    }
}
