using System.Globalization;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public sealed class JobSystem : SharedJobSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JobComponent, MindRoleAddedEvent>(MindOnDoGreeting);
    }

    private void MindOnDoGreeting(EntityUid mindId, JobComponent component, ref MindRoleAddedEvent args)
    {
        if (args.Silent)
            return;

        if (!_mind.TryGetSession(mindId, out var session))
            return;

        if (!MindTryGetJob(mindId, out _, out var prototype))
            return;

        // TODO this should probably get moved into briefing somehow, or at least a more prominent area
        if (prototype.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));
    }

    public void MindAddJob(EntityUid mindId, string jobPrototypeId)
    {
        if (MindHasJobWithId(mindId, jobPrototypeId))
            return;

        _roles.MindAddRole(mindId, new JobComponent { Prototype = jobPrototypeId });
    }
}
