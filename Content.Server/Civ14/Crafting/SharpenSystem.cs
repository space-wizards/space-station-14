using Content.Server.DoAfter;
using Content.Shared.Crafting;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Kitchen.Components;

namespace Content.Server.Crafting;

public sealed partial class SharpenSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharpenableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SharpenableComponent, SharpenDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(EntityUid uid, SharpenableComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SharpComponent>(args.Used))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.SharpenTime, new SharpenDoAfterEvent(), uid, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, SharpenableComponent component, ref SharpenDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var sharpenedStick = Spawn(component.ResultPrototype, Transform(uid).MapPosition);

        QueueDel(uid);

        args.Handled = true;
    }
}