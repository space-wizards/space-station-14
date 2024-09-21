using Content.Server.Mind;
using Content.Shared.Examine;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;

namespace Content.Server.Corvax.HiddenDescription;

public sealed partial class HiddenDescriptionSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> hiddenDesc, ref ExaminedEvent args)
    {
        _mind.TryGetMind(args.Examiner, out var mindId, out var mindComponent);
        TryComp<JobComponent>(mindId, out var job);

        foreach (var item in hiddenDesc.Comp.Entries)
        {
            var isJobAllow = job?.Prototype != null && item.JobRequired.Contains(job.Prototype.Value);
            var isMindWhitelistPassed = _whitelistSystem.IsValid(item.WhitelistMind, mindId);
            var isBodyWhitelistPassed = _whitelistSystem.IsValid(item.WhitelistMind, args.Examiner);
            var passed = item.NeedAllCheck
                ? isMindWhitelistPassed && isBodyWhitelistPassed && isJobAllow
                : isMindWhitelistPassed || isBodyWhitelistPassed || isJobAllow;

            if (passed)
                args.PushMarkup(Loc.GetString(item.Label), hiddenDesc.Comp.PushPriority);
        }
    }
}