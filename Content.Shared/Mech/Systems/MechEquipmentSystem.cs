using Content.Shared.Interaction;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechEquipmentSystem : MechInstallSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechEquipmentComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechEquipmentComponent, InsertEquipmentEvent>(OnInsert);
    }

    private void OnUsed(Entity<MechEquipmentComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryPrepareInstall(args.User, mech, out var mechComp))
            return;

        if (mechComp == null)
            return;

        if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
        {
            Popup.PopupPredicted(Loc.GetString("mech-equipment-slot-full-popup"), args.User, args.User);
            return;
        }

        if (Whitelist.IsWhitelistFail(mechComp.EquipmentWhitelist, ent.Owner))
        {
            Popup.PopupPredicted(Loc.GetString("mech-equipment-whitelist-fail-popup"), args.User, args.User);
            return;
        }

        if (HasDuplicateInstalled(ent.Owner, mechComp.EquipmentContainer.ContainedEntities, args.User))
            return;

        StartInstallDoAfter(args.User, ent.Owner, mech, ent.Comp.InstallDuration, new InsertEquipmentEvent());
    }

    private void OnInsert(Entity<MechEquipmentComponent> ent, ref InsertEquipmentEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        var mech = args.Args.Target.Value;
        if (!TryFinalizeInsert(mech, out var mechComp))
            return;

        PopupFinish(mech, ent.Owner);
        MechSystem.InsertEquipment((mech, mechComp!), ent.Owner, equipmentComponent: ent.Comp);
        args.Handled = true;
    }
}
