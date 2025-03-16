using Content.Server.DoAfter;
using Content.Shared.Rocks;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Rocks;

public sealed partial class KnappingSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnappingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<KnappingComponent, KnappingDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, KnappingComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<KnappingAnchoredComponent>(args.Target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.HitTime, new KnappingDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(EntityUid flintUid, KnappingComponent component, ref KnappingDoAfterEvent args)
    {

        if (args.Cancelled || args.Handled)
            return;

        component.CurrentHits++;

        if (component.CurrentHits >= component.HitsRequired)
        {
            var result = Spawn(component.ResultPrototype, Transform(flintUid).MapPosition);
            QueueDel(flintUid);
            _hands.TryPickupAnyHand(args.Args.User, result);
        }

        args.Handled = true;
    }
}