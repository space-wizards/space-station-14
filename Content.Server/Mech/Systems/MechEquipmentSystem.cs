using Content.Server.DoAfter;
using Content.Server.Mech.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech.Equipment.Components;

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
        SubscribeLocalEvent<MechEquipmentComponent, DoAfterEvent<InsertEquipmentEvent>>(OnInsertEquipment);
    }

    private void OnUsed(EntityUid uid, MechEquipmentComponent component, AfterInteractEvent args)
    {
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

        var insertEquipment = new InsertEquipmentEvent();
        var doAfterEventArgs = new DoAfterEventArgs(args.User, component.InstallDuration, target: mech, used: uid)
        {
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true
        };

        _doAfter.DoAfter(doAfterEventArgs, insertEquipment);
    }

    private void OnInsertEquipment(EntityUid uid, MechEquipmentComponent component, DoAfterEvent<InsertEquipmentEvent> args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Args.Target.Value);
        _mech.InsertEquipment(args.Args.Target.Value, uid);

        args.Handled = true;
    }

    private sealed class InsertEquipmentEvent : EntityEventArgs
    {

    }
}
