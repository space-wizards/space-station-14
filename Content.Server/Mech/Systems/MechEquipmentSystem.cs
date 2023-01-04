using Content.Server.DoAfter;
using Content.Server.Mech.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Mech.Equipment.Components;
using Robust.Shared.Player;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechEquipmentSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
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

        if (mechComp.Broken)
            return;

        if (args.User == mechComp.PilotSlot.ContainedEntity)
            return;

        if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
            return;

        if (mechComp.EquipmentWhitelist != null && !mechComp.EquipmentWhitelist.IsValid(uid))
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", uid)), mech);

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
        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Mech);
        _mech.InsertEquipment(args.Mech, uid);
    }

    private void OnCancelled(EntityUid uid, MechEquipmentComponent component, MechEquipmentInstallCancelled args)
    {
        component.TokenSource = null;
    }
}
