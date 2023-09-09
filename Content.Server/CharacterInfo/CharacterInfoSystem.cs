using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Systems;

namespace Content.Server.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly ObjectiveSystem _objective = default!;
    [Dependency] private readonly RoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue
            || args.SenderSession.AttachedEntity != msg.EntityUid)
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        var objectives = new Dictionary<string, List<ObjectiveInfo>>();
        var jobTitle = "No Profession";
        string? briefing = null;
        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            // Get objectives
            foreach (var objective in mind.AllObjectives)
            {
                // group objectives by their issuer
                var issuer = objective.Prototype.Issuer;
                if (!objectives.ContainsKey(issuer))
                    objectives[issuer] = new List<ObjectiveInfo>();

                var info = _objective.GetConditionInfo(objective, mindId, mind);
                objectives[issuer].Add(info);
            }

            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                jobTitle = jobName;

            // Get briefing
            briefing = _roles.MindGetBriefing(mindId);
        }

        RaiseNetworkEvent(new CharacterInfoEvent(entity, jobTitle, objectives, briefing), args.SenderSession);
    }
}
