using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;

namespace Content.Shared.Electrocution;

public sealed partial class InsulatedSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InsulatedComponent, InventoryRelayedEvent<ShotAttemptedEvent>>(OnShoot);
    }
    private void OnShoot(EntityUid uid, InsulatedComponent comp, ref InventoryRelayedEvent<ShotAttemptedEvent> args)
    {
        if (comp.PreventOpperatinGuns && !args.Args.Used.Comp.BigTrigger)
        {
            PopupSystem.PopupClient(Loc.GetString("gun-Insulated-gloves"), args.Args.User);
            args.Args.Cancel();
        }
    }
}
