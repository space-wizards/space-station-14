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
        base.Initialize();

        SubscribeLocalEvent<SimpleToolUsageComponent, AfterInteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<SimpleToolUsageComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(Entity<SimpleToolUsageComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!_tools.HasQuality(args.Used, ent.Comp.Quality))
            return;

        AttemptToolUsage(ent, args.User, args.Used);
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
                if (used != null)
                    AttemptToolUsage(ent, user, used.Value);
            },
            Disabled = disabled,
            Message = disabled ? Loc.GetString(ent.Comp.BlockedMessage, ("quality", ent.Comp.Quality)) : null,
            Text = Loc.GetString(ent.Comp.UsageVerb),
        };

        args.Verbs.Add(verb);
    }

    private void AttemptToolUsage(Entity<SimpleToolUsageComponent> ent, EntityUid user, EntityUid tool)
    {
        var attemptEv = new AttemptSimpleToolUseEvent(user);
        RaiseLocalEvent(ent, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfter, new SimpleToolDoAfterEvent(), ent, tool)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }
}
