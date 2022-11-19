using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Mech.Equipment.Components;
using Robust.Shared.Player;

namespace Content.Server.Mech;

public sealed class MechEquipmentSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechEquipmentComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechEquipmentComponent, MechEquipmentInstallFinished>(OnFinished);
        SubscribeLocalEvent<MechEquipmentComponent, MechEquipmentInstallCancelled>(OnCancelled);
    }

    private void OnUsed(EntityUid uid, MechEquipmentComponent component, AfterInteractEvent args)
    {
        if (component.TokenSource != null)
            return;

        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<MechComponent>(mech, out var mechComp))
            return;

        //TODO: check for valid equipment here.

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", uid)), mech, Filter.Pvs(mech));

        component.TokenSource = new();
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.InstallDuration, component.TokenSource.Token, mech, uid)
        {
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            UsedFinishedEvent = new MechEquipmentInstallFinished(mech),
            UsedCancelledEvent = new MechEquipmentInstallCancelled()
        });
    }

    private void OnFinished(EntityUid uid, MechEquipmentComponent component, MechEquipmentInstallFinished args)
    {
        component.TokenSource = null;
        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Mech, Filter.Pvs(args.Mech));

        if (!TryComp<MechComponent>(args.Mech, out var mechComp))
            return;
        mechComp.EquipmentContainer.Insert(uid, EntityManager);
    }

    private void OnCancelled(EntityUid uid, MechEquipmentComponent component, MechEquipmentInstallCancelled args)
    {
        component.TokenSource = null;
    }
}
