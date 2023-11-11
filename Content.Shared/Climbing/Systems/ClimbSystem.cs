using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Climbing.Systems;

public sealed partial class ClimbSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    private const string ClimbingFixtureName = "climb";
    private const int ClimbingCollisionGroup = (int) (CollisionGroup.TableLayer | CollisionGroup.LowImpassable);

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<ClimbingComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<ClimbingComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ClimbingComponent, ClimbDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ClimbingComponent, EndCollideEvent>(OnClimbEndCollide);
        SubscribeLocalEvent<ClimbingComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<ClimbingComponent, EntityUnpausedEvent>(OnClimbableUnpaused);

        SubscribeLocalEvent<ClimbableComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbableVerb);
        SubscribeLocalEvent<ClimbableComponent, DragDropTargetEvent>(OnClimbableDragDrop);

        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
    }

    private void OnClimbableUnpaused(EntityUid uid, ClimbingComponent component, ref EntityUnpausedEvent args)
    {
        if (component.NextTransition == null)
            return;

        component.NextTransition = component.NextTransition.Value + args.PausedTime;
        Dirty(uid, component);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ClimbingComponent>();
        var curTime = _timing.CurTime;

        // Move anything still climb in the specified direction.
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextTransition == null)
                continue;

            if (comp.NextTransition < curTime)
            {
                FinishTransition(uid, comp);
                continue;
            }

            var xform = _xformQuery.GetComponent(uid);
            _xformSystem.SetLocalPosition(uid, xform.LocalPosition + comp.Direction * frameTime, xform);
        }
    }

    private void FinishTransition(EntityUid uid, ClimbingComponent comp)
    {
        // TODO: Validate climb here
        comp.NextTransition = null;
        _actionBlockerSystem.UpdateCanMove(uid);
        Dirty(uid, comp);

        // Stop if necessary.
        if (!_fixturesQuery.TryGetComponent(uid, out var fixtures) ||
            !IsClimbing(uid, fixtures))
        {
            StopClimb(uid, comp);
            return;
        }
    }

    /// <summary>
    /// Returns true if entity currently has a valid vault.
    /// </summary>
    private bool IsClimbing(EntityUid uid, FixturesComponent? fixturesComp = null)
    {
        if (!_fixturesQuery.Resolve(uid, ref fixturesComp) || !fixturesComp.Fixtures.TryGetValue(ClimbingFixtureName, out var climbFixture))
            return false;

        foreach (var contact in climbFixture.Contacts.Values)
        {
            var other = uid == contact.EntityA ? contact.EntityB : contact.EntityA;

            if (HasComp<ClimbableComponent>(other))
            {
                return true;
            }
        }

        return false;
    }

    private void OnMoveAttempt(EntityUid uid, ClimbingComponent component, UpdateCanMoveEvent args)
    {
        // Can't move when transition.
        if (component.NextTransition != null)
            args.Cancel();
    }

    private void OnParentChange(EntityUid uid, ClimbingComponent component, ref EntParentChangedMessage args)
    {
        if (component.NextTransition != null)
        {
            FinishTransition(uid, component);
        }
    }

     private void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, ref CanDropTargetEvent args)
     {
         if (args.Handled)
             return;

         var canVault = args.User == args.Dragged
             ? CanVault(component, args.User, uid, out _)
             : CanVault(component, args.User, args.Dragged, uid, out _);

         args.CanDrop = canVault;
         args.Handled = true;
     }

     private void AddClimbableVerb(EntityUid uid, ClimbableComponent component, GetVerbsEvent<AlternativeVerb> args)
     {
         if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
             return;

         if (!TryComp(args.User, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
             return;

         // TODO VERBS ICON add a climbing icon?
         args.Verbs.Add(new AlternativeVerb
         {
             Act = () => TryClimb(args.User, args.User, args.Target, out _, component),
             Text = Loc.GetString("comp-climbable-verb-climb")
         });
     }

     private void OnClimbableDragDrop(EntityUid uid, ClimbableComponent component, ref DragDropTargetEvent args)
     {
         // definitely a better way to check if two entities are equal
         // but don't have computer access and i have to do this without syntax
         if (args.Handled || args.User != args.Dragged && !HasComp<HandsComponent>(args.User))
             return;

         TryClimb(args.User, args.Dragged, uid, out _, component);
     }

     public bool TryClimb(
         EntityUid user,
         EntityUid entityToMove,
         EntityUid climbable,
         out DoAfterId? id,
         ClimbableComponent? comp = null,
         ClimbingComponent? climbing = null)
     {
         id = null;

         if (!Resolve(climbable, ref comp) || !Resolve(entityToMove, ref climbing))
             return false;

         // Note, IsClimbing does not mean a DoAfter is active, it means the target has already finished a DoAfter and
         // is currently on top of something..
         if (climbing.IsClimbing)
             return true;

         var args = new DoAfterArgs(EntityManager, user, comp.ClimbDelay, new ClimbDoAfterEvent(),
             entityToMove,
             target: climbable,
             used: entityToMove)
         {
             BreakOnTargetMove = true,
             BreakOnUserMove = true,
             BreakOnDamage = true
         };

         _audio.PlayPredicted(comp.StartClimbSound, climbable, user);
         return _doAfterSystem.TryStartDoAfter(args, out id);
     }

     private void OnDoAfter(EntityUid uid, ClimbingComponent component, ClimbDoAfterEvent args)
     {
         if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
             return;

         Climb(uid, args.Args.User, args.Args.Target.Value, climbing: component);
         args.Handled = true;
     }

     private void Climb(EntityUid uid, EntityUid user, EntityUid climbable, bool silent = false, ClimbingComponent? climbing = null,
         PhysicsComponent? physics = null, FixturesComponent? fixtures = null, ClimbableComponent? comp = null)
     {
         if (!Resolve(uid, ref climbing, ref physics, ref fixtures, false))
             return;

         if (!Resolve(climbable, ref comp))
             return;

         if (!ReplaceFixtures(uid, climbing, fixtures))
             return;

         var xform = _xformQuery.GetComponent(uid);
         var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(xform);
         var worldDirection = _xformSystem.GetWorldPosition(climbable) - worldPos;
         var distance = worldDirection.Length();
         var parentRot = (worldRot - xform.LocalRotation);
         // Need direction relative to climber's parent.
         var localDirection = (-parentRot).RotateVec(worldDirection);

         // On top of it already so just do it in place.
         if (localDirection.LengthSquared() < 0.01f)
         {
             climbing.NextTransition = null;
         }
         // VirtualController over to the thing.
         else
         {
             var climbDuration = TimeSpan.FromSeconds(distance / climbing.TransitionRate);
             climbing.NextTransition = _timing.CurTime + climbDuration;

             climbing.Direction = localDirection.Normalized() * climbing.TransitionRate;
             _actionBlockerSystem.UpdateCanMove(uid);
         }

         climbing.IsClimbing = true;
         Dirty(uid, climbing);

         _audio.PlayPredicted(comp.FinishClimbSound, climbable, user);

         var startEv = new StartClimbEvent(climbable);
         var climbedEv = new ClimbedOnEvent(uid, user);
         RaiseLocalEvent(uid, ref startEv);
         RaiseLocalEvent(climbable, ref climbedEv);

         if (silent)
             return;

         string selfMessage;
         string othersMessage;

         if (user == uid)
         {
             othersMessage = Loc.GetString("comp-climbable-user-climbs-other",
                 ("user", Identity.Entity(uid, EntityManager)),
                 ("climbable", climbable));

             selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", climbable));
         }
         else
         {
             othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                 ("user", Identity.Entity(user, EntityManager)),
                 ("moved-user", Identity.Entity(uid, EntityManager)), ("climbable", climbable));

             selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", Identity.Entity(uid, EntityManager)),
                 ("climbable", climbable));
         }

         _popupSystem.PopupEntity(othersMessage, uid, Filter.PvsExcept(user, entityManager: EntityManager), true);
         _popupSystem.PopupClient(selfMessage, uid, user);
     }

     /// <summary>
     /// Replaces the current fixtures with non-climbing collidable versions so that climb end can be detected
     /// </summary>
     /// <returns>Returns whether adding the new fixtures was successful</returns>
     private bool ReplaceFixtures(EntityUid uid, ClimbingComponent climbingComp, FixturesComponent fixturesComp)
     {
         // Swap fixtures
         foreach (var (name, fixture) in fixturesComp.Fixtures)
         {
             if (climbingComp.DisabledFixtureMasks.ContainsKey(name)
                 || fixture.Hard == false
                 || (fixture.CollisionMask & ClimbingCollisionGroup) == 0)
             {
                 continue;
             }

             climbingComp.DisabledFixtureMasks.Add(name, fixture.CollisionMask & ClimbingCollisionGroup);
             _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask & ~ClimbingCollisionGroup, fixturesComp);
         }

         if (!_fixtureSystem.TryCreateFixture(
                 uid,
                 new PhysShapeCircle(0.35f),
                 ClimbingFixtureName,
                 collisionLayer: (int) CollisionGroup.None,
                 collisionMask: ClimbingCollisionGroup,
                 hard: false,
                 manager: fixturesComp))
         {
             return false;
         }

         return true;
     }

     private void OnClimbEndCollide(EntityUid uid, ClimbingComponent component, ref EndCollideEvent args)
     {
         if (args.OurFixtureId != ClimbingFixtureName
             || !component.IsClimbing
             || component.NextTransition != null)
         {
             return;
         }

         foreach (var fixture in args.OurFixture.Contacts.Keys)
         {
             if (fixture == args.OtherFixture)
                 continue;

             // If still colliding with a climbable, do not stop climbing
             if (HasComp<ClimbableComponent>(args.OtherEntity))
                 return;
         }

         StopClimb(uid, component);
     }

     private void StopClimb(EntityUid uid, ClimbingComponent? climbing = null, FixturesComponent? fixtures = null)
     {
         if (!Resolve(uid, ref climbing, ref fixtures, false))
             return;

         foreach (var (name, fixtureMask) in climbing.DisabledFixtureMasks)
         {
             if (!fixtures.Fixtures.TryGetValue(name, out var fixture))
             {
                 continue;
             }

             _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask | fixtureMask, fixtures);
         }

         climbing.DisabledFixtureMasks.Clear();
         _fixtureSystem.DestroyFixture(uid, ClimbingFixtureName, manager: fixtures);
         climbing.IsClimbing = false;
         climbing.NextTransition = null;
         var ev = new EndClimbEvent();
         RaiseLocalEvent(uid, ref ev);
         Dirty(uid, climbing);
     }

     /// <summary>
     ///     Checks if the user can vault the target
     /// </summary>
     /// <param name="component">The component of the entity that is being vaulted</param>
     /// <param name="user">The entity that wants to vault</param>
     /// <param name="target">The object that is being vaulted</param>
     /// <param name="reason">The reason why it cant be dropped</param>
     public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid target, out string reason)
     {
         if (!_actionBlockerSystem.CanInteract(user, target))
         {
             reason = Loc.GetString("comp-climbable-cant-interact");
             return false;
         }

         if (!HasComp<ClimbingComponent>(user)
             || !TryComp(user, out BodyComponent? body)
             || !_bodySystem.BodyHasPartType(user, BodyPartType.Leg, body)
             || !_bodySystem.BodyHasPartType(user, BodyPartType.Foot, body))
         {
             reason = Loc.GetString("comp-climbable-cant-climb");
             return false;
         }

         if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range))
         {
             reason = Loc.GetString("comp-climbable-cant-reach");
             return false;
         }

         reason = string.Empty;
         return true;
     }

     /// <summary>
     ///     Checks if the user can vault the dragged entity onto the the target
     /// </summary>
     /// <param name="component">The climbable component of the object being vaulted onto</param>
     /// <param name="user">The user that wants to vault the entity</param>
     /// <param name="dragged">The entity that is being vaulted</param>
     /// <param name="target">The object that is being vaulted onto</param>
     /// <param name="reason">The reason why it cant be dropped</param>
     /// <returns></returns>
     public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target,
         out string reason)
     {
         if (!_actionBlockerSystem.CanInteract(user, dragged) || !_actionBlockerSystem.CanInteract(user, target))
         {
             reason = Loc.GetString("comp-climbable-cant-interact");
             return false;
         }

         if (!HasComp<ClimbingComponent>(dragged))
         {
             reason = Loc.GetString("comp-climbable-cant-climb");
             return false;
         }

         bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

         if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
             || !_interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored))
         {
             reason = Loc.GetString("comp-climbable-cant-reach");
             return false;
         }

         reason = string.Empty;
         return true;
     }

     public void ForciblySetClimbing(EntityUid uid, EntityUid climbable, ClimbingComponent? component = null)
     {
         Climb(uid, uid, climbable, true, component);
     }

     private void OnBuckleChange(EntityUid uid, ClimbingComponent component, ref BuckleChangeEvent args)
     {
         if (!args.Buckling)
             return;
         StopClimb(uid, component);
     }

     private void OnGlassClimbed(EntityUid uid, GlassTableComponent component, ref ClimbedOnEvent args)
     {
         if (TryComp<PhysicsComponent>(args.Climber, out var physics) && physics.Mass <= component.MassLimit)
             return;

         _damageableSystem.TryChangeDamage(args.Climber, component.ClimberDamage, origin: args.Climber);
         _damageableSystem.TryChangeDamage(uid, component.TableDamage, origin: args.Climber);
         _stunSystem.TryParalyze(args.Climber, TimeSpan.FromSeconds(component.StunTime), true);

         // Not shown to the user, since they already get a 'you climb on the glass table' popup
         _popupSystem.PopupEntity(
             Loc.GetString("glass-table-shattered-others", ("table", uid), ("climber", Identity.Entity(args.Climber, EntityManager))), args.Climber,
             Filter.PvsExcept(args.Climber), true);
     }

    [Serializable, NetSerializable]
    private sealed partial class ClimbDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
