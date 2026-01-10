using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Burial.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Botany.Items.Systems;

/// <summary>
/// System for using a shovel on a plant.
/// </summary>
public sealed class BotanyShovelSystem : EntitySystem
{
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShovelComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, TrayShovelAttemptEvent>(OnTrayShovelAttempt);
    }

    private void OnAfterInteract(Entity<ShovelComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new TrayShovelAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnTrayShovelAttempt(Entity<PlantTrayComponent> ent, ref TrayShovelAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
        {
            _popup.PopupPredictedCursor(
                Loc.GetString("plant-holder-component-no-plant-message", ("name", ent.Owner)),
                args.User);
            return;
        }

        _popup.PopupPredictedCursor(
            Loc.GetString("plant-holder-component-remove-plant-message", ("name", ent.Owner)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(
            Loc.GetString("plant-holder-component-remove-plant-others-message",
                ("name", Identity.Entity(args.User, EntityManager))),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);

        _plant.RemovePlant(plantUid.Value);
    }
}

