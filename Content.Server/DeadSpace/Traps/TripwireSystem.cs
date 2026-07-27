// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeviceLinking.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Stealth;
using Content.Shared.DeadSpace.Traps;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Explosion.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;
using System.Numerics;

namespace Content.Server.DeadSpace.Traps;

public sealed class TripwireSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _links = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TripwireComponent, StepTriggerAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TripwireComponent, StepTriggeredOffEvent>(OnStepped);
        SubscribeLocalEvent<TripwireComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TripwireComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TripwireComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TripwireComponent, TripwireDisarmDoAfterEvent>(OnDisarm);
        SubscribeLocalEvent<TripwireComponent, TripwireAttachExplosiveDoAfterEvent>(OnAttachExplosive);
        SubscribeLocalEvent<TripwireComponent, TripwireDetachExplosiveDoAfterEvent>(OnDetachExplosive);
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

        if (HasComp<TimerTriggerComponent>(args.Used))
        {
            if (ent.Comp.LinkedTargets.Contains(args.Used))
            {
                args.Handled = true;
                return;
            }

            var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.AttachTime,
                new TripwireAttachExplosiveDoAfterEvent(), ent, target: ent, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                DistanceThreshold = 2f,
            };
            args.Handled = _doAfter.TryStartDoAfter(doAfter);
            return;
        }

        args.Handled = _tools.UseTool(args.Used, args.User, ent, ent.Comp.DisarmTime,
            [SharedToolSystem.CutQuality], new TripwireDisarmDoAfterEvent());
    }

    private void OnInteractHand(Entity<TripwireComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.Triggered || !HasAttachedExplosive(ent.Comp))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DetachExplosiveTime,
            new TripwireDetachExplosiveDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 2f,
        };
        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnAttachExplosive(Entity<TripwireComponent> ent, ref TripwireAttachExplosiveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || ent.Comp.Triggered || args.Used is not { } explosive ||
            !HasComp<TimerTriggerComponent>(explosive) || ent.Comp.LinkedTargets.Contains(explosive))
            return;

        args.Handled = true;
        if (!_hands.TryDrop(args.User, explosive))
            return;

        _transform.SetCoordinates(explosive, Transform(ent).Coordinates);
        _transform.AnchorEntity(explosive);
        ent.Comp.LinkedTargets.Add(explosive);
        ent.Comp.AttachedExplosives.Add(explosive);

        var stealth = EnsureComp<StealthComponent>(explosive);
        _stealth.SetEnabled(explosive, true, stealth);
        _stealth.SetVisibility(explosive, 1f, stealth);

        var fade = EnsureComp<StealthOnMoveComponent>(explosive);
        fade.PassiveVisibilityRate = ent.Comp.AttachedExplosiveFadeTime > 0f
            ? -2f / ent.Comp.AttachedExplosiveFadeTime
            : -2f;
        fade.MovementVisibilityRate = 0f;
        Dirty(explosive, fade);
    }

    private void OnDetachExplosive(Entity<TripwireComponent> ent, ref TripwireDetachExplosiveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || ent.Comp.Triggered)
            return;

        EntityUid? explosive = null;
        foreach (var attached in ent.Comp.AttachedExplosives)
        {
            if (!Deleted(attached))
            {
                explosive = attached;
                break;
            }
        }

        if (explosive == null)
            return;

        args.Handled = true;
        ent.Comp.AttachedExplosives.Remove(explosive.Value);
        ent.Comp.LinkedTargets.Remove(explosive.Value);
        _transform.Unanchor(explosive.Value);
        RemComp<StealthOnMoveComponent>(explosive.Value);
        RemComp<StealthComponent>(explosive.Value);
        _hands.TryPickup(args.User, explosive.Value);
    }

    private bool HasAttachedExplosive(TripwireComponent component)
    {
        foreach (var explosive in component.AttachedExplosives)
        {
            if (!Deleted(explosive))
                return true;
        }

        return false;
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

                if (segment.Comp.AttachedExplosives.Contains(target))
                {
                    // Some grenades have AnchorOnTrigger and must be unanchored before
                    // receiving their trigger, otherwise the snap-grid gets a duplicate.
                    _transform.Unanchor(target);
                }

                // Explosives must detonate immediately. Routing an already armed C4
                // through its timer trigger can delete it before the explosion is processed.
                if (TryComp<ExplosiveComponent>(target, out var explosive))
                    _explosion.TriggerExplosive(target, explosive, user: user);
                else
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
