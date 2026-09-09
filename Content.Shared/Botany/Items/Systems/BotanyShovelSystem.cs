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
public sealed partial class BotanyShovelSystem : EntitySystem
{
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<PlantComponent> _plantQuery = default!;
    [Dependency] private EntityQuery<PlantTrayComponent> _trayQuery = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<ShovelComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        // Allow interacting with either the plant or the tray.
        var target = args.Target.Value;
        if (_plantQuery.HasComp(target))
        {
            if (!_plant.TryGetTray(target, out var tray))
                return;

            target = tray.Owner;
        }
        else if (!_trayQuery.HasComp(target))
            return;

        var ev = new TrayShovelAttemptEvent(ent, args.User);
        RaiseLocalEvent(target, ref ev);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnTrayShovelAttempt(Entity<PlantTrayComponent> ent, ref TrayShovelAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
        {
            _popup.PopupCursor(
                Loc.GetString("plant-shovel-component-no-plant-popup", ("name", ent.Owner)),
                args.User);
            return;
        }

        _popup.PopupCursor(
            Loc.GetString("plant-shovel-component-remove-plant-popup", ("name", ent.Owner)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(
            Loc.GetString("plant-shovel-component-remove-plant-others-popup",
                ("name", Identity.Entity(args.User, EntityManager))),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);

        _plant.RemovePlant(plantUid.Value);
    }
}
