using Content.Server.Popups;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Content.Shared.Interaction;

namespace Content.Server.Crayon;

public sealed partial class MagicCrayonSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicCrayonComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, MagicCrayonComponent component, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (_charges.IsEmpty(uid))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), uid, args.User);
            QueueDel(uid);
            args.Handled = true;
            return;
        }

        Spawn(component.SpawnProto, args.ClickLocation);
        _charges.TryUseCharge(uid);
        args.Handled = true;
    }
}
