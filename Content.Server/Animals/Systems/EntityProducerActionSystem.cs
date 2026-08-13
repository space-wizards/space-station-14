using Content.Server.Animals.Components;
using Content.Server.Popups;
using Content.Shared.Animals.Events;
using Content.Shared.IdentityManagement;
using Robust.Server.Audio;

namespace Content.Server.Animals.Systems;

/// <summary>
/// Handles entity production actions and their feedback.
/// </summary>
public sealed partial class EntityProducerActionSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HungerProductionSystem _hungerProduction = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnProductionAction(Entity<EntityProducerActionComponent> ent, ref EntityProductionActionEvent args)
    {
        args.Handled = _hungerProduction.TryProduce(ent.Owner, out var failure);
        if (failure == HungerProductionFailure.Hungry)
            _popup.PopupEntity(Loc.GetString(ent.Comp.TooHungryPopup), ent.Owner, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnEntitiesProduced(Entity<EntityProducerActionComponent> ent, ref EntitiesProducedEvent args)
    {
        _audio.PlayPvs(ent.Comp.ProductionSound, args.Owner);
        _popup.PopupEntity(
            Loc.GetString(ent.Comp.UserPopup),
            Loc.GetString(ent.Comp.OthersPopup, ("entity", Identity.Entity(args.Owner, EntityManager))),
            args.Owner,
            args.Owner);
    }
}
