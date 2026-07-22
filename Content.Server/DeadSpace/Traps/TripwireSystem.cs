// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeadSpace.Traps;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Tools.Systems;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;
using System.Numerics;

namespace Content.Server.DeadSpace.Traps;

public sealed class TripwireSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _links = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TripwireComponent, StepTriggerAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TripwireComponent, StepTriggeredOffEvent>(OnStepped);
        SubscribeLocalEvent<TripwireComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TripwireComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TripwireComponent, TripwireDisarmDoAfterEvent>(OnDisarm);
    }

    private void OnNewLink(Entity<TripwireComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source == ent.Owner && args.SourcePort == ent.Comp.Port)
            ent.Comp.LinkedTargets.Add(args.Sink);
    }

    private void OnAttempt(Entity<TripwireComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !ent.Comp.Triggered && !IsIgnored(ent, args.Tripper);
    }

    private void OnStepped(Entity<TripwireComponent> ent, ref StepTriggeredOffEvent args)
    {
        Trigger(ent, args.Tripper);
    }

    private void OnInteractUsing(Entity<TripwireComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.Triggered || args.Handled)
            return;

        args.Handled = _tools.UseTool(args.Used, args.User, ent, ent.Comp.DisarmTime,
            [SharedToolSystem.CutQuality], new TripwireDisarmDoAfterEvent());
    }

    private void OnDisarm(Entity<TripwireComponent> ent, ref TripwireDisarmDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            Trigger(ent, args.User);
            return;
        }

        if (!args.Handled)
            QueueDel(ent);
        args.Handled = true;
    }

    private bool IsIgnored(EntityUid trap, EntityUid target)
    {
        if (!TryComp<TrapIgnoreComponent>(trap, out var ignore))
            return false;

        if (ignore.Whitelist != null && _whitelist.IsValid(ignore.Whitelist, target))
            return true;

        return ignore.Factions.Count > 0 &&
               TryComp<NpcFactionMemberComponent>(target, out var member) &&
               _factions.IsMemberOfAny((target, member), ignore.Factions);
    }

    private void Trigger(Entity<TripwireComponent> ent, EntityUid user)
    {
        if (ent.Comp.Triggered)
            return;

        var network = GetConnectedNetwork(ent);
        foreach (var segment in network)
            segment.Comp.Triggered = true;

        foreach (var segment in network)
        {
            foreach (var target in segment.Comp.LinkedTargets)
            {
                if (Deleted(target))
                    continue;
                _trigger.Trigger(target, user, segment.Comp.ImmediateTriggerKey);
            }

            TryComp<DeviceLinkSourceComponent>(segment, out var source);
            _links.InvokePort(segment, segment.Comp.Port, sourceComponent: source);
        }

        foreach (var segment in network)
            QueueDel(segment);
    }

    private List<Entity<TripwireComponent>> GetConnectedNetwork(Entity<TripwireComponent> origin)
    {
        var result = new List<Entity<TripwireComponent>>();
        var visited = new HashSet<EntityUid> { origin };
        var queue = new Queue<Entity<TripwireComponent>>();
        queue.Enqueue(origin);

        while (queue.TryDequeue(out var current))
        {
            result.Add(current);
            var currentXform = Transform(current);
            var query = EntityQueryEnumerator<TripwireComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var tripwire, out var xform))
            {
                if (visited.Contains(uid) || xform.ParentUid != currentXform.ParentUid)
                    continue;

                var delta = xform.LocalPosition - currentXform.LocalPosition;
                var horizontal = MathF.Abs(delta.X) <= 1.01f && MathF.Abs(delta.Y) <= 0.01f;
                var vertical = MathF.Abs(delta.Y) <= 1.01f && MathF.Abs(delta.X) <= 0.01f;
                if ((!horizontal && !vertical) || delta == Vector2.Zero)
                    continue;

                visited.Add(uid);
                queue.Enqueue((uid, tripwire));
            }
        }

        return result;
    }
}
