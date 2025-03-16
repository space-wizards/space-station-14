using Content.Server.DoAfter;
using Content.Shared.Crafting;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Kitchen.Components;
using Content.Shared.Verbs;

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
        SubscribeLocalEvent<SharpenableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, SharpenableComponent component, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !component.CanSharpenByHand)
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = "Sharpen",
            Act = () => StartSharpeningByHand(uid, component, user)
        };
        args.Verbs.Add(verb);
    }

    // Sharpening by hand, if possible, takes twice as much time
    private void StartSharpeningByHand(EntityUid uid, SharpenableComponent component, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.SharpenTime * 2, new SharpenDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
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