using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles the insertion of mech module into mechs.
/// </summary>
public sealed class MechModuleSystem : MechInstallSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechModuleComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechModuleComponent, InsertModuleEvent>(OnInsert);
    }

    private void OnUsed(EntityUid uid, MechModuleComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryPrepareInstall(uid, args.User, mech, out var mechComp))
            return;

        if (mechComp == null)
            return;

        if (mechComp.ModuleContainer.ContainedEntities.Count >= mechComp.MaxModuleAmount)
        {
            Popup.PopupEntity(Loc.GetString("mech-module-slot-full-popup"), args.User);
            return;
        }

        if (Whitelist.IsWhitelistFail(mechComp.ModuleWhitelist, uid))
        {
            Popup.PopupEntity(Loc.GetString("mech-module-whitelist-fail-popup"), args.User);
            return;
        }

        if (HasDuplicateInstalled(uid, mechComp.ModuleContainer.ContainedEntities, args.User))
            return;

        StartInstallDoAfter(args.User, uid, mech, component.InstallDuration, new InsertModuleEvent());
    }

    private void OnInsert(EntityUid uid, MechModuleComponent component, InsertModuleEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        var mech = args.Args.Target.Value;
        if (!TryFinalizeInsert(mech, args.Args.User, out var mechComp))
            return;

        PopupFinish(mech, uid);
        MechSystem.InsertEquipment(mech, uid, mechComp, moduleComponent: component);
        args.Handled = true;
    }
}
