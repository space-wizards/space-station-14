using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.RPGoals;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.RPGoals;

public sealed class RPGoalAssignmentSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<NetUserId, RPGoalSession> _sessions = new();
    private readonly IRPGoalStorage _storage = new NullRPGoalStorage();

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeNetworkEvent<RPGoalAcceptMessage>(OnAcceptGoal);
        SubscribeNetworkEvent<RPGoalRerollMessage>(OnRerollGoal);
        SubscribeNetworkEvent<RPGoalSelectionRequest>(OnSelectionRequested);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var context = GetRoleContext(args.Player, args.JobId);
        if (context == null)
            return;

        PrepareSession(args.Player.UserId, context);
    }

    public void PrepareSession(NetUserId userId, PlayerRoleContext context)
    {
        if (_sessions.TryGetValue(userId, out var existing) && existing.Finalized)
            return;

        var session = new RPGoalSession
        {
            UserId = context.UserId,
            RoleId = context.RoleId,
        };

        RollOptions(session, context);
        _sessions[userId] = session;
    }

    public bool TryOpenSelectionUi(ICommonSession session)
    {
        if (!_sessions.TryGetValue(session.UserId, out var goalSession) || goalSession.Finalized)
            return false;

        RaiseNetworkEvent(new RPGoalSelectionState(goalSession.CurrentOptions, goalSession.RerollsRemaining, goalSession.Finalized), session.Channel);
        return true;
    }

    private void OnSelectionRequested(RPGoalSelectionRequest _, EntitySessionEventArgs args)
    {
        TryOpenSelectionUi(args.SenderSession);
    }

    private void OnRerollGoal(RPGoalRerollMessage _, EntitySessionEventArgs args)
    {
        if (!_sessions.TryGetValue(args.SenderSession.UserId, out var session) || session.Finalized)
            return;

        if (session.RerollsRemaining <= 0)
            return;

        var context = GetRoleContext(args.SenderSession, session.RoleId);
        if (context == null)
            return;

        session.RerollsRemaining -= 1;
        RollOptions(session, context);
        RaiseNetworkEvent(new RPGoalSelectionState(session.CurrentOptions, session.RerollsRemaining, session.Finalized), args.SenderSession.Channel);
    }

    private void OnAcceptGoal(RPGoalAcceptMessage msg, EntitySessionEventArgs args)
    {
        if (!_sessions.TryGetValue(args.SenderSession.UserId, out var session) || session.Finalized)
            return;

        var selected = session.CurrentOptions.FirstOrDefault(opt => string.Equals(opt.GoalId, msg.GoalId, StringComparison.OrdinalIgnoreCase));
        if (selected == null)
            return;

        session.Finalized = true;
        session.SelectedGoalId = selected.GoalId;
        PersistSelection(args.SenderSession.UserId, selected);
        _storage.SaveSelection(args.SenderSession.UserId, session);

        _chat.DispatchServerMessage(args.SenderSession,
            Loc.GetString("rp-goals-selection-confirmed", ("goal", Loc.GetString(selected.LocaleKey))));

        RaiseNetworkEvent(new RPGoalSelectionState(session.CurrentOptions, session.RerollsRemaining, true), args.SenderSession.Channel);
    }

    private void RollOptions(RPGoalSession session, PlayerRoleContext context)
    {
        var pool = GetEligibleGoals(context)
            .Where(goal => !session.SeenGoalIds.Contains(goal.ID))
            .ToList();

        if (pool.Count == 0)
            pool = GetEligibleGoals(context).ToList();

        session.CurrentOptions.Clear();

        foreach (var goal in PickWeightedDistinct(pool, 3))
        {
            session.SeenGoalIds.Add(goal.ID);
            session.CurrentOptions.Add(new RPGoalOption(goal.ID, goal.LocaleKey, goal.Category));
        }
    }

    private IEnumerable<RPGoalPrototype> GetEligibleGoals(PlayerRoleContext context)
    {
        foreach (var goal in _prototype.EnumeratePrototypes<RPGoalPrototype>())
        {
            if (goal.AllowedRoles.Count > 0 && !goal.AllowedRoles.Contains(context.RoleId))
                continue;

            if (goal.BlockedRoles.Contains(context.RoleId))
                continue;

            if (goal.UnsafeTags.Overlaps(RPGoalUnsafePolicy.DefaultForbiddenTags))
                continue;

            if (goal.ForbiddenTags.Overlaps(RPGoalUnsafePolicy.DefaultForbiddenTags))
                continue;

            if (goal.Requirements.MinRoundMinutes is { } minRound && context.RoundMinutes < minRound)
                continue;

            if (goal.Requirements.Department is { } department && !string.Equals(department, context.Department, StringComparison.OrdinalIgnoreCase))
                continue;

            if (goal.Requirements.RequiredJobTags.Count > 0 && !goal.Requirements.RequiredJobTags.IsSubsetOf(context.JobTags))
                continue;

            if (goal.Requirements.ExcludedJobTags.Overlaps(context.JobTags))
                continue;

            yield return goal;
        }
    }

    private IEnumerable<RPGoalPrototype> PickWeightedDistinct(List<RPGoalPrototype> pool, int count)
    {
        var working = new List<RPGoalPrototype>(pool);

        for (var i = 0; i < count && working.Count > 0; i++)
        {
            var total = working.Sum(g => MathF.Max(0.01f, g.Weight));
            var roll = _random.NextFloat() * total;
            var acc = 0f;

            for (var index = 0; index < working.Count; index++)
            {
                var candidate = working[index];
                acc += MathF.Max(0.01f, candidate.Weight);
                if (roll > acc)
                    continue;

                working.RemoveAt(index);
                yield return candidate;
                break;
            }
        }
    }

    private void PersistSelection(NetUserId userId, RPGoalOption selected)
    {
        if (!_mind.TryGetMind(userId, out var mindId, out var mind))
            return;

        mind.RPGoalId = selected.GoalId;
        mind.RPGoalLocaleKey = selected.LocaleKey;
        Dirty(mindId.Value, mind);
    }

    private PlayerRoleContext? GetRoleContext(ICommonSession session, string? fallbackJobId)
    {
        var roleId = fallbackJobId;

        if (_mind.TryGetMind(session, out _, out var mindComp))
        {
            foreach (var roleEnt in mindComp.MindRoleContainer.ContainedEntities)
            {
                if (!HasComp<JobRoleComponent>(roleEnt) || !TryComp<MindRoleComponent>(roleEnt, out var jobRole) || string.IsNullOrWhiteSpace(jobRole.JobPrototype))
                    continue;

                roleId = jobRole.JobPrototype;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(roleId) || !_prototype.TryIndex<JobPrototype>(roleId, out var job))
            return null;

        string? departmentId = null;
        var jobTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            roleId,
        };

        foreach (var department in _prototype.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!department.Roles.Contains(roleId))
                continue;

            jobTags.Add(department.ID);
            if (department.Primary)
                departmentId ??= department.ID;
        }

        departmentId ??= _prototype.EnumeratePrototypes<DepartmentPrototype>()
            .FirstOrDefault(department => department.Roles.Contains(roleId))?.ID;

        return new PlayerRoleContext(
            session.UserId.ToString(),
            roleId,
            departmentId,
            jobTags,
            (int) _gameTicker.RoundDuration().TotalMinutes);
    }
}
