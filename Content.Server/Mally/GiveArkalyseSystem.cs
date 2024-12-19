using Content.Shared.Interaction.Events;
using Content.Server.Mally.Arkalyse.Components;
using Robust.Server.GameObjects;
using Content.Shared.DoAfter;
using Content.Shared.Arkalyse;

namespace Content.Server.Mally;

public sealed class GiveArkalyseSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GiveArkalyseComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GiveArkalyseComponent, ArkalyseDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(EntityUid uid, GiveArkalyseComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ArkalyseStunComponent>(args.User) || HasComp<ArkalyseDamageComponent>(args.User) || HasComp<ArkalyseMutedComponent>(args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.LearnTime, new ArkalyseDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, GiveArkalyseComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        EnsureComp<ArkalyseStunComponent>(args.Args.User);
        EnsureComp<ArkalyseDamageComponent>(args.Args.User);
        EnsureComp<ArkalyseMutedComponent>(args.Args.User);

        TransformToItem(uid, component);
    }

    private void TransformToItem(EntityUid item, GiveArkalyseComponent component)
    {
        var position = _transform.GetMapCoordinates(item);
        Del(item);
        Spawn("Ash", position);
    }
}


