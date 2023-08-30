using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;

namespace Content.Server.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
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

        var conditions = new Dictionary<string, List<ConditionInfo>>();
        var jobTitle = "No Profession";
        var briefing = "!!ERROR: No Briefing!!"; //should never show on the UI unless there's an issue
        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            // Get objectives
            foreach (var objective in mind.AllObjectives)
            {
                if (!conditions.ContainsKey(objective.Prototype.Issuer))
                    conditions[objective.Prototype.Issuer] = new List<ConditionInfo>();
                foreach (var condition in objective.Conditions)
                {
                    conditions[objective.Prototype.Issuer].Add(new ConditionInfo(condition.Title,
                        condition.Description, condition.Icon, condition.Progress));
                }
            }

            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                jobTitle = jobName;

            // Get briefing
            briefing = _roles.MindGetBriefing(mindId) ?? string.Empty;
        }

        RaiseNetworkEvent(new CharacterInfoEvent(entity, jobTitle, conditions, briefing), args.SenderSession);
    }
}
