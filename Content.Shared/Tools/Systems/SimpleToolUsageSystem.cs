using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Systems;

public sealed partial class SimpleToolUsageSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleToolUsageComponent, AfterInteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<SimpleToolUsageComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(Entity<SimpleToolUsageComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!_tools.HasQuality(args.Used, ent.Comp.Quality))
            return;

        var evattempt = new AttemptSimpleToolUseEvent(args.User);
        RaiseLocalEvent(ent, ref evattempt);

        if (evattempt.Cancelled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfter, new SimpleToolDoAfterEvent(), ent, args.Used)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    public void OnGetInteractionVerbs(Entity<SimpleToolUsageComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (ent.Comp.UsageVerb == null)
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        var disabled = args.Using == null || !_tools.HasQuality(args.Using.Value, ent.Comp.Quality);

        var used = args.Using;
        var user = args.User;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                var ev = new AttemptSimpleToolUseEvent(user);
                RaiseLocalEvent(ent, ref ev);

                if (ev.Cancelled)
                    return;

                var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfter, new SimpleToolDoAfterEvent(), ent, used)
                {
                    BreakOnDamage = true,
                    BreakOnDropItem = true,
                    BreakOnMove = true,
                    BreakOnHandChange = true,
                };

                _doAfterSystem.TryStartDoAfter(doAfterArgs);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Disabled = disabled,
            Text = Loc.GetString(ent.Comp.UsageVerb),
        };

        args.Verbs.Add(verb);
    }
}
