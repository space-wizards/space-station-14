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
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;

namespace Content.Server.Climbing;

[UsedImplicitly]
public sealed class ClimbSystem : SharedClimbSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;

    private const string ClimbingFixtureName = "climb";

    private readonly Dictionary<EntityUid, List<Fixture>> _fixtureRemoveQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbVerb);
        SubscribeLocalEvent<ClimbingComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
        SubscribeLocalEvent<ClimbableComponent, DragDropEvent>(OnDragDrop);
        SubscribeLocalEvent<ClimbableComponent, ComponentInit>(OnClimbableInit);
        SubscribeLocalEvent<ClimbingComponent, ClimbFinishedEvent>(OnClimbFinished);
        SubscribeLocalEvent<ClimbingComponent, EndCollideEvent>(OnClimbEndCollide);
    }

    private void OnClimbEndCollide(EntityUid uid, ClimbingComponent component, EndCollideEvent args)
    {
        if (args.OurFixture.ID != ClimbingFixtureName || !component.IsClimbing || component.OwnerIsTransitioning
            || !TryComp<TransformComponent>(uid, out var transformComp)
            || !TryComp<PhysicsComponent>(uid, out var physicsComp)
            || !TryComp<FixturesComponent>(uid, out var fixturesComp))
            return;

        foreach (var fixture in component.DisabledFixtures)
        {
            _fixtureSystem.CreateFixture(physicsComp, fixture, true, fixturesComp, transformComp);
        }
        component.DisabledFixtures.Clear();

        if (!_fixtureRemoveQueue.TryGetValue(uid, out var removeQueue))
        {
            removeQueue = new List<Fixture>();
            _fixtureRemoveQueue.Add(uid, removeQueue);
        }

        foreach (var name in component.Fixtures)
        {
            if (!fixturesComp.Fixtures.TryGetValue(name, out var fixture))
                continue;
            removeQueue.Add(fixture);
        }
        component.Fixtures.Clear();

        if (fixturesComp.Fixtures.TryGetValue(ClimbingFixtureName, out var climbingFixture))
            removeQueue.Add(climbingFixture);

        component.IsClimbing = false;
    }

    private void OnClimbFinished(EntityUid uid, ClimbingComponent climbingComp, ClimbFinishedEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComp) || !TryComp<FixturesComponent>(uid, out var fixturesComp))
            return;

        // Swap fixtures
        var toAdd = new List<Fixture>();
        foreach (var (name, fixture) in fixturesComp.Fixtures)
        {
            if (climbingComp.Fixtures.Contains(name)
                || climbingComp.Fixtures.Contains($"{ClimbingFixtureName}-{name}")
                || fixture.Hard == false || (fixture.CollisionMask & (int) CollisionGroup.MobImpassable) == 0)
                continue;

            climbingComp.DisabledFixtures.Add(fixture);

            var climbFixture = new Fixture(fixture.Shape, fixture.CollisionLayer,
                    fixture.CollisionMask & ~(int)(CollisionGroup.MobImpassable | CollisionGroup.VaultImpassable), true)
                { ID = $"{ClimbingFixtureName}-{name}" };
            toAdd.Add(climbFixture);
            climbingComp.Fixtures.Add(climbFixture.ID);
        }

        foreach (var fixture in climbingComp.DisabledFixtures)
        {
            _fixtureSystem.DestroyFixture(physicsComp, fixture, manager:fixturesComp);
        }

        foreach (var fixture in toAdd)
        {
            _fixtureSystem.TryCreateFixture(physicsComp, fixture, manager:fixturesComp);
        }

        if (!_fixtureSystem.TryCreateFixture(physicsComp,
                new Fixture(new PhysShapeCircle(), (int) CollisionGroup.None,
                    (int) CollisionGroup.MobImpassable, false) { ID = ClimbingFixtureName},
                manager: fixturesComp))
            return;

        // Calculate moveTo
        var entityPos = Transform(uid).WorldPosition;

        var endPoint = Transform(args.Climbable).WorldPosition;
        var (x, y) = (endPoint - entityPos).Normalized;

        var climbMode = Comp<ClimbingComponent>(uid);
        climbMode.IsClimbing = true;

        if (MathF.Abs(x) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
            endPoint = new Vector2(entityPos.X, endPoint.Y);
        else if (MathF.Abs(y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
            endPoint = new Vector2(endPoint.X, entityPos.Y);

        TryMoveTo(uid, climbingComp, entityPos, endPoint);
        // we may potentially need additional logic since we're forcing a player onto a climbable
        // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

        RaiseLocalEvent(uid, new StartClimbEvent(args.Climbable), false);
        RaiseLocalEvent(args.Climbable, new ClimbedOnEvent(uid), false);

        if (args.User == uid)
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-other", ("user", uid),
                ("climbable", args.Climbable));
            uid.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", args.Climbable));
            uid.PopupMessage(selfMessage);
        }
        else
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                ("user", args.User), ("moved-user", uid), ("climbable", args.Climbable));
            args.User.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", uid), ("climbable", args.Climbable));
            args.User.PopupMessage(selfMessage);
        }
    }

    private void OnClimbableInit(EntityUid uid, ClimbableComponent component, ComponentInit args)
    {
        EntityManager.EnsureComponent<PhysicsComponent>(uid);
    }

    protected override void OnCanDragDropOn(EntityUid uid, SharedClimbableComponent component, CanDragDropOnEvent args)
    {
        base.OnCanDragDropOn(uid, component, args);

        if (!args.CanDrop)
            return;

        string reason;
        bool canVault;

        if (args.User == args.Dragged)
            canVault = CanVault(component, args.User, args.Target, out reason);
        else
            canVault = CanVault(component, args.User, args.Dragged, args.Target, out reason);

        if (!canVault)
            args.User.PopupMessage(reason);

        args.CanDrop = canVault;
        args.Handled = true;
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
        if (!_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(user) ||
            !TryComp(user, out SharedBodyComponent? body))
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

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range))
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
    /// <param name="component">The climbable component of the object being vaulted onto</param>
    /// <param name="user">The user that wants to vault the entity</param>
    /// <param name="dragged">The entity that is being vaulted</param>
    /// <param name="target">The object that is being vaulted onto</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    private bool CanVault(SharedClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target, out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, dragged))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        // CanInteract() doesn't support checking a second "target" entity.
        // Doing so manually:
        var ev = new GettingInteractedWithAttemptEvent(user, target);
        RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
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

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored) ||
            !_interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private void OnDragDrop(EntityUid uid, ClimbableComponent component, DragDropEvent args)
    {
        TryMoveEntity(component, args.User, args.Dragged, args.Target);
    }

    private void TryMoveEntity(ClimbableComponent component, EntityUid user, EntityUid entityToMove, EntityUid climbable)
    {
        if (!TryComp(entityToMove, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
            return;

        _doAfterSystem.DoAfter(new DoAfterEventArgs(entityToMove, component.ClimbDelay, default, climbable)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            UserFinishedEvent = new ClimbFinishedEvent(user, climbable),
        });
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
        if (!TryComp(args.User, out ClimbingComponent? climbingComponent) ||
            climbingComponent.IsClimbing)
            return;

        // Add a climb verb
        AlternativeVerb verb = new();
        verb.Act = () => TryMoveEntity(component, args.User, args.User, args.Target);
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

    private void UnsetTransitionBoolAfterBufferTime(EntityUid uid, ClimbingComponent? climbing = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref climbing, ref physics, false))
            return;
        climbing.Owner.SpawnTimer((int) (SharedClimbingComponent.BufferTime * 1000), () =>
        {
            if (climbing.Deleted) return;
            physics.BodyType = BodyType.KinematicController;
            climbing.OwnerIsTransitioning = false;
            _actionBlockerSystem.UpdateCanMove(uid);
        });
    }

    /// <summary>
    /// Make the owner climb from one point to another
    /// </summary>
    public void TryMoveTo(EntityUid uid, ClimbingComponent component, Vector2 from, Vector2 to)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComponent)) return;

        var velocity = (to - from).Length;

        if (velocity <= 0.0f) return;

        // Since there are bodies with different masses:
        // mass * 5 seems enough to move entity
        // instead of launching cats like rockets against the walls with constant impulse value.
        physicsComponent.ApplyLinearImpulse((to - from).Normalized * velocity * physicsComponent.Mass * 10);
        physicsComponent.BodyType = BodyType.Dynamic;
        component.OwnerIsTransitioning = true;
        _actionBlockerSystem.UpdateCanMove(uid);

        UnsetTransitionBoolAfterBufferTime(uid, component);
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, fixtures) in _fixtureRemoveQueue)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physicsComp)
                || !TryComp<FixturesComponent>(uid, out var fixturesComp))
                continue;
            foreach (var fixture in fixtures)
            {
                _fixtureSystem.DestroyFixture(physicsComp, fixture, true, fixturesComp);
            }
        }
        _fixtureRemoveQueue.Clear();

        // foreach (var climber in _activeClimbers.ToArray())
        // {
        //     climber.Update();
        // }
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        _fixtureRemoveQueue.Clear();
    }
}

internal sealed class ClimbFinishedEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public EntityUid Climbable { get; }

    public ClimbFinishedEvent(EntityUid user, EntityUid climbable)
    {
        User = user;
        Climbable = climbable;
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
