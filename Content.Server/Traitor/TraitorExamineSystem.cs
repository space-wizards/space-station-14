using Content.Server.Mind.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Server.Traitor;

public sealed class TraitorExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, TraitorExamineComponent comp, ExaminedEvent args)
    {
        // observers and other traitors can see examine messages
        if (HasComp<SharedGhostComponent>(args.Examiner) || (args.IsInDetailsRange && IsTraitor(args.Examiner)))
            args.PushMarkup(Loc.GetString(comp.Message));
    }

    private bool IsTraitor(EntityUid user)
    {
        // TODO: if/when mind is refactored, change to use role id + put id in the component
        return TryComp<MindComponent>(user, out var mind) && (mind.Mind?.HasRole<TraitorRole>() ?? false);
    }
}
