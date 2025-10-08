using Content.Server.Chat.Managers;
using Content.Server.Objectives.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Chat.Systems;
using Robust.Server.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;

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
        var query = EntityQueryEnumerator<PickRandomPersonComponent>();

        //called infrequently so its probably fine
        while (query.MoveNext(out var uid, out var picker))
        {
            if (!picker.RerollsCryostorage)
                continue;

            // invalid objective prototype
            if (!TryComp<TargetObjectiveComponent>(uid, out var target))
                continue;

            // couldn't find a target :(
            if (_mind.PickFromPool(picker.Pool, picker.Filters, uid) is not {} picked)
                continue;

            _target.SetTarget(uid, picked, target);
            _target.ChangeTitle(uid, target, MetaData(uid));

            if (_player.TryGetSessionByEntity(uid, out var session))
            {
                _audio.PlayGlobal(picker.RerollSound, session);
                var msg = Loc.GetString(picker.RerollText);
                var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
                _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, picker.RerollColor);;
            }
        }
    }
}
