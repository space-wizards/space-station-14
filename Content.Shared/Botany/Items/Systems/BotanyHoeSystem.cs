using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Botany.Items.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanyHoeSystem : EntitySystem
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BotanyHoeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, TrayHoeAttemptEvent>(OnTrayHoeAttempt);
    }

    private void OnAfterInteract(Entity<BotanyHoeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new TrayHoeAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnTrayHoeAttempt(Entity<PlantTrayComponent> ent, ref TrayHoeAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.WeedLevel <= 0)
        {
            _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            return;
        }

        _popup.PopupPredictedCursor(
            Loc.GetString("plant-holder-component-remove-weeds-message",
                ("name", ent.Owner)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(
            Loc.GetString("plant-holder-component-remove-weeds-others-message",
                ("otherName", Identity.Entity(args.User, EntityManager))),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);

        _plantTray.AdjustWeed(ent.AsNullable(), -args.Hoe.Comp.WeedAmount);
    }
}
