using Content.Server.Chat.Managers;
using Content.Server.Objectives.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Player;
using Content.Shared.Roles.Jobs;
using Robust.Server.Audio;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetObjectiveComponent"/> using different components.
/// These can be combined with condition components for objective completions in order to create a variety of objectives.
/// </summary>
public sealed class PickObjectiveTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickSpecificPersonComponent, ObjectiveAssignedEvent>(OnSpecificPersonAssigned);
        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnRandomPersonAssigned);
        SubscribeLocalEvent<CryostorageEnteredEvent>(OnRandomReassign);
    }

    private void OnSpecificPersonAssigned(Entity<PickSpecificPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }

    private void OnRandomPersonAssigned(Entity<PickRandomPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // couldn't find a target :(
        if (_mind.PickFromPool(ent.Comp.Pool, ent.Comp.Filters, args.MindId) is not {} picked)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent, picked, target);
    }

    private void OnRandomReassign(ref CryostorageEnteredEvent ev)
    {
        var query = EntityQueryEnumerator<RepickOnCryoComponent>();

        //called infrequently so its probably fine
        while (query.MoveNext(out var uid, out var repicker))
        {
            // nothing to filter!!
            if (!TryComp<PickRandomPersonComponent>(uid, out var picker))
                continue;

            // invalid objective prototype
            if (!TryComp<TargetObjectiveComponent>(uid, out var targetObjective))
                continue;

            // only change targets if yours just cryod
            if (targetObjective.Target != ev.SleepyUid)
                continue;

            // find the mind responsible for this objective, as to not make it the new target + for the text and audio playing
            var mindQuery = EntityQueryEnumerator<MindComponent>();
            while (mindQuery.MoveNext(out var playerMindId, out var playerMind))
            {
                if (!playerMind.Objectives.Contains(uid))
                    continue;

                // find a new target, keeps old target if no viable ones were found
                if (_mind.PickFromPool(picker.Pool, picker.Filters, playerMindId) is not {} targetMind)
                    break;

                // set the new target id and name for the objective
                _target.SetTarget(uid, targetMind, targetObjective);
                _target.ChangeTitle(uid, targetObjective, MetaData(uid));

                if (!_player.TryGetSessionById(playerMind.UserId, out var session))
                    break;

                // tell the player their objectives changed
                _audio.PlayGlobal(repicker.RerollSound, session);

                var targetName = targetMind.Comp.CharacterName;
                if (targetName == null)
                    targetName = "Unknown";

                var job = _job.MindTryGetJobName(targetMind);
                var msg = Loc.GetString(repicker.RerollText, ("Name", targetName), ("Job", job));
                var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));

                _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, repicker.RerollColor);
                _adminLog.Add(LogType.Mind, LogImpact.Low, $"Objective target changed in mind of {_mind.MindOwnerLoggingString(playerMind)} to {targetName}");
            }
        }
    }
}
