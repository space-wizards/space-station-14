using Content.Server.Popups;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using System.ComponentModel;

namespace Content.Server.Crayon;

public sealed partial class MagicCrayonSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicCrayonComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MagicCrayonComponent, MagicCrayonDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MagicCrayonComponent component, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target != null)
            return;

        if (_charges.IsEmpty(uid))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(2.0f),
            new MagicCrayonDoAfterEvent(), uid, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, MagicCrayonComponent component, ref MagicCrayonDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target != null)
            return;

        if (_charges.IsEmpty(uid))
            return;

        Spawn(component.SpawnProto, Transform(args.User).Coordinates);
        _charges.TryUseCharge(uid);

        if (_charges.IsEmpty(uid))
            UseUp(uid, args.User);

        args.Handled = true;
    }

    private void UseUp(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity("The magic crayon has been used up.", user);
        QueueDel(uid);
    }
}
