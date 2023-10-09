using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.CharacterInfo;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
        SubscribeNetworkEvent<RequestAntagonistInfoEvent>(OnRequestAntagonistInfoEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue || args.SenderSession.AttachedEntity != GetEntity(msg.NetEntity))
            return;

        var entity = args.SenderSession.AttachedEntity.Value;
        var jobTitle = "No Profession";
        var objectives = new Dictionary<string, List<ObjectiveInfo>>();
        string? briefing = null;

        if (_minds.TryGetMind(entity, out var mindId, out var mind))
        {
            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                jobTitle = jobName;

            GetObjectives(mindId, mind, objectives);

            // Get briefing
            briefing = _roles.MindGetBriefing(mindId);
        }

        RaiseNetworkEvent(new CharacterInfoEvent(GetNetEntity(entity), jobTitle, objectives, briefing), args.SenderSession);
    }

    private void OnRequestAntagonistInfoEvent(RequestAntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return;

        var receiver = args.SenderSession.AttachedEntity.Value;
        var antagonist = msg.NetEntity;
        var jobTitle = "No Profession";
        var objectives = new Dictionary<string, List<ObjectiveInfo>>();

        if (_minds.TryGetMind(GetEntity(antagonist), out var mindId, out var mind))
        {
            if (_jobs.MindTryGetJobName(mindId, out var jobName))
                jobTitle = jobName;

            GetObjectives(mindId, mind, objectives);
        }

        RaiseNetworkEvent(new AntagonistInfoEvent(GetNetEntity(receiver), antagonist, jobTitle, objectives), args.SenderSession);
    }

    private void GetObjectives([NotNullWhen(true)] EntityUid mindId, [NotNullWhen(true)] MindComponent mind, Dictionary<string, List<ObjectiveInfo>> objectives)
    {
        foreach (var objective in mind.AllObjectives)
        {
            var info = _objectives.GetInfo(objective, mindId, mind);
            if (info == null)
                continue;

            // group objectives by their issuer
            var issuer = Comp<ObjectiveComponent>(objective).Issuer;
            if (!objectives.ContainsKey(issuer))
                objectives[issuer] = new List<ObjectiveInfo>();
            objectives[issuer].Add(info.Value);
        }
    }
}
