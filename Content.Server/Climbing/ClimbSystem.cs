using System.Linq;
using Content.Server.Climbing.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Climbing;

[UsedImplicitly]
internal sealed class ClimbSystem : SharedClimbSystem
{
    private readonly HashSet<ClimbingComponent> _activeClimbers = new();

    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbVerb);
        SubscribeLocalEvent<ClimbingComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
        SubscribeLocalEvent<ClimbableComponent, DragDropEvent>(OnDragDrop);
        SubscribeLocalEvent<ClimbableComponent, ComponentInit>(OnClimbableInit);
    }

    private void OnClimbableInit(EntityUid uid, ClimbableComponent component, ComponentInit args)
    {
        EntityManager.EnsureComponent<PhysicsComponent>(uid);
    }

    protected override void OnCanDragDropOn(EntityUid uid, SharedClimbableComponent component, CanDragDropOnEvent args)
    {
        base.OnCanDragDropOn(uid, component, args);

        if (!args.Handled)
            return;

        string reason;
        bool canVault;

        if (args.User == args.Dragged)
            canVault = CanVault(component, args.User, args.Target, out reason);
        else
            canVault = CanVault(component, args.User, args.Dragged, args.Target, out reason);

        if (!canVault)
            args.User.PopupMessage(reason);

        args.Handled = canVault;
    }

    /// <summary>
    /// Checks if the user can vault the target
    /// </summary>
    /// <param name="user">The entity that wants to vault</param>
    /// <param name="target">The object that is being vaulted</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    private bool CanVault(SharedClimbableComponent component, EntityUid user, EntityUid target, out string reason)
    {
        if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!EntityManager.HasComponent<ClimbingComponent>(user) ||
            !EntityManager.TryGetComponent(user, out SharedBodyComponent? body))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        if (!body.HasPartOfType(BodyPartType.Leg) ||
            !body.HasPartOfType(BodyPartType.Foot))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(user, target, component.Range))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Checks if the user can vault the dragged entity onto the the target
    /// </summary>
    /// <param name="user">The user that wants to vault the entity</param>
    /// <param name="dragged">The entity that is being vaulted</param>
    /// <param name="target">The object that is being vaulted onto</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    private bool CanVault(SharedClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target, out string reason)
    {
        if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user, dragged))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        // CanInteract() doesn't support checking a second "target" entity.
        // Doing so manually:
        var ev = new GettingInteractedWithAttemptEvent(user, target);
        EntityManager.EventBus.RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!EntityManager.HasComponent<ClimbingComponent>(dragged))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        var sys = EntitySystem.Get<SharedInteractionSystem>();
        if (!sys.InRangeUnobstructed(user, target, component.Range, predicate: Ignored) ||
            !sys.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private void OnDragDrop(EntityUid uid, ClimbableComponent component, DragDropEvent args)
    {
        if (args.User == args.Dragged)
        {
            TryClimb(component, args.User, args.Target);
        }
        else
        {
            TryMoveEntity(component, args.User, args.Dragged, args.Target);
        }
    }

    private async void TryMoveEntity(ClimbableComponent component, EntityUid user, EntityUid entityToMove, EntityUid climbable)
    {
        var doAfterEventArgs = new DoAfterEventArgs(user, component.ClimbDelay, default, entityToMove)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true
        };

        var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

        if (result != DoAfterStatus.Cancelled && EntityManager.TryGetComponent(entityToMove, out FixturesComponent? fixtureComp) && fixtureComp.FixtureCount >= 1)
        {
            var entityPos = EntityManager.GetComponent<TransformComponent>(entityToMove).WorldPosition;

            var direction = (EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition - entityPos).Normalized;
            var endPoint = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition;

            var climbMode = EntityManager.GetComponent<ClimbingComponent>(entityToMove);
            climbMode.IsClimbing = true;

            if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
            {
                endPoint = new Vector2(entityPos.X, endPoint.Y);
            }
            else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
            {
                endPoint = new Vector2(endPoint.X, entityPos.Y);
            }

            climbMode.TryMoveTo(entityPos, endPoint);
            // we may potentially need additional logic since we're forcing a player onto a climbable
            // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

            EntityManager.EventBus.RaiseLocalEvent(entityToMove, new StartClimbEvent(climbable), false);
            EntityManager.EventBus.RaiseLocalEvent(climbable, new ClimbedOnEvent(entityToMove), false);

            var othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                ("user", user), ("moved-user", entityToMove), ("climbable", component.Owner));
            user.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", entityToMove), ("climbable", component.Owner));
            user.PopupMessage(selfMessage);
        }
    }

    public async void TryClimb(ClimbableComponent component, EntityUid entityToMove, EntityUid climbable)
    {
        if (!EntityManager.TryGetComponent(entityToMove, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
            return;

        var doAfterEventArgs = new DoAfterEventArgs(entityToMove, component.ClimbDelay, default, component.Owner)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true
        };

        var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

        if (result != DoAfterStatus.Cancelled && EntityManager.TryGetComponent(entityToMove, out FixturesComponent? fixtureComp) && fixtureComp.FixtureCount >= 1)
        {
            // TODO: Remove the copy-paste code
            var entityPos = EntityManager.GetComponent<TransformComponent>(entityToMove).WorldPosition;

            var direction = (EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition - entityPos).Normalized;
            var endPoint = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition;

            var climbMode = EntityManager.GetComponent<ClimbingComponent>(entityToMove);
            climbMode.IsClimbing = true;

            if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
            {
                endPoint = new Vector2(entityPos.X, endPoint.Y);
            }
            else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
            {
                endPoint = new Vector2(endPoint.X, entityPos.Y);
            }

            climbMode.TryMoveTo(entityPos, endPoint);

            EntityManager.EventBus.RaiseLocalEvent(entityToMove, new StartClimbEvent(climbable), false);
            EntityManager.EventBus.RaiseLocalEvent(climbable, new ClimbedOnEvent(entityToMove), false);

            var othersMessage = Loc.GetString("comp-climbable-user-climbs-other", ("user", entityToMove), ("climbable", component.Owner));
            entityToMove.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", component.Owner));
            entityToMove.PopupMessage(selfMessage);
        }
    }

    public void ForciblySetClimbing(EntityUid uid, ClimbingComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;
        component.IsClimbing = true;
        UnsetTransitionBoolAfterBufferTime(uid, component);
    }

    private void AddClimbVerb(EntityUid uid, ClimbableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
            return;

        // Check that the user climb.
        if (!EntityManager.TryGetComponent(args.User, out ClimbingComponent? climbingComponent) ||
            climbingComponent.IsClimbing)
            return;

        // Add a climb verb
        AlternativeVerb verb = new();
        verb.Act = () => TryClimb(component, args.User, args.Target);
        verb.Text = Loc.GetString("comp-climbable-verb-climb");
        // TODO VERBS ICON add a climbing icon?
        args.Verbs.Add(verb);
    }

    private void OnBuckleChange(EntityUid uid, ClimbingComponent component, BuckleChangeEvent args)
    {
        if (args.Buckling)
            component.IsClimbing = false;
    }

    private void OnGlassClimbed(EntityUid uid, GlassTableComponent component, ClimbedOnEvent args)
    {
        if (TryComp<PhysicsComponent>(args.Climber, out var physics) && physics.Mass <= component.MassLimit)
            return;
        _damageableSystem.TryChangeDamage(args.Climber, component.ClimberDamage);
        _damageableSystem.TryChangeDamage(uid, component.TableDamage);
        _stunSystem.TryParalyze(args.Climber, TimeSpan.FromSeconds(component.StunTime), true);

        // Not shown to the user, since they already get a 'you climb on the glass table' popup
        _popupSystem.PopupEntity(Loc.GetString("glass-table-shattered-others",
                ("table", uid), ("climber", args.Climber)), args.Climber,
            Filter.Pvs(uid).RemoveWhereAttachedEntity(puid => puid == args.Climber));
    }

    public void AddActiveClimber(ClimbingComponent climbingComponent)
    {
        _activeClimbers.Add(climbingComponent);
    }

    public void RemoveActiveClimber(ClimbingComponent climbingComponent)
    {
        _activeClimbers.Remove(climbingComponent);
    }

    public void UnsetTransitionBoolAfterBufferTime(EntityUid uid, ClimbingComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;
        component.Owner.SpawnTimer((int) (SharedClimbingComponent.BufferTime * 1000), () =>
        {
            if (component.Deleted) return;
            component.OwnerIsTransitioning = false;
        });
    }

    public override void Update(float frameTime)
    {
        foreach (var climber in _activeClimbers.ToArray())
        {
            climber.Update();
        }
    }

    public void Reset(RoundRestartCleanupEvent ev)
    {
        _activeClimbers.Clear();
    }
}

/// <summary>
///     Raised on an entity when it is climbed on.
/// </summary>
public sealed class ClimbedOnEvent : EntityEventArgs
{
    public EntityUid Climber;

    public ClimbedOnEvent(EntityUid climber)
    {
        Climber = climber;
    }
}

/// <summary>
///     Raised on an entity when it successfully climbs on something.
/// </summary>
public sealed class StartClimbEvent : EntityEventArgs
{
    public EntityUid Climbable;

    public StartClimbEvent(EntityUid climbable)
    {
        Climbable = climbable;
    }
}
