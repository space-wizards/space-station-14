using System.Numerics;
using Content.Shared.Database;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Movement.Events;
using Content.Shared.Physics.Pull;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Follower;

public sealed class FollowerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedJointSystem _jointSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<FollowerComponent, MoveInputEvent>(OnFollowerMove);
        SubscribeLocalEvent<FollowerComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<FollowerComponent, EntityTerminatingEvent>(OnFollowerTerminating);

        SubscribeLocalEvent<FollowerComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<FollowedComponent, EntityTerminatingEvent>(OnFollowedTerminating);
        SubscribeLocalEvent<BeforeSaveEvent>(OnBeforeSave);
    }

    private void OnBeforeSave(BeforeSaveEvent ev)
    {
        // Some followers will not be map savable. This ensures that maps don't get saved with empty/invalid
        // followers, but just stopping any following on the map being saved.

        var query = AllEntityQuery<FollowerComponent, TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var follower, out var xform, out var meta))
        {
            if (meta.EntityPrototype == null || meta.EntityPrototype.MapSavable)
                continue;

            if (xform.MapUid != ev.Map)
                continue;

            StopFollowingEntity(uid, follower.Following);
        }
    }

    private void OnGetAlternativeVerbs(GetVerbsEvent<AlternativeVerb> ev)
    {
        if (ev.User == ev.Target || IsClientSide(ev.Target))
            return;

        if (HasComp<GhostComponent>(ev.User))
        {
            var verb = new AlternativeVerb()
            {
                Priority = 10,
                Act = () => StartFollowingEntity(ev.User, ev.Target),
                Impact = LogImpact.Low,
                Text = Loc.GetString("verb-follow-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"))
            };
            ev.Verbs.Add(verb);
        }

        if (_tagSystem.HasTag(ev.Target, "ForceableFollow"))
        {
            if (!ev.CanAccess || !ev.CanInteract)
                return;

            var verb = new AlternativeVerb
            {
                Priority = 10,
                Act = () => StartFollowingEntity(ev.Target, ev.User),
                Impact = LogImpact.Low,
                Text = Loc.GetString("verb-follow-me-text"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
            };

            ev.Verbs.Add(verb);
        }
    }

    private void OnFollowerMove(EntityUid uid, FollowerComponent component, ref MoveInputEvent args)
    {
        StopFollowingEntity(uid, component.Following);
    }

    private void OnPullStarted(EntityUid uid, FollowerComponent component, PullStartedMessage args)
    {
        StopFollowingEntity(uid, component.Following);
    }

    private void OnGotEquippedHand(EntityUid uid, FollowerComponent component, GotEquippedHandEvent args)
    {
        StopFollowingEntity(uid, component.Following, deparent:false);
    }

    private void OnFollowerTerminating(EntityUid uid, FollowerComponent component, ref EntityTerminatingEvent args)
    {
        StopFollowingEntity(uid, component.Following, deparent: false);
    }

    // Since we parent our observer to the followed entity, we need to detach
    // before they get deleted so that we don't get recursively deleted too.
    private void OnFollowedTerminating(EntityUid uid, FollowedComponent component, ref EntityTerminatingEvent args)
    {
        StopAllFollowers(uid, component);
    }

    /// <summary>
    ///     Makes an entity follow another entity, by parenting to it.
    /// </summary>
    /// <param name="follower">The entity that should follow</param>
    /// <param name="entity">The entity to be followed</param>
    public void StartFollowingEntity(EntityUid follower, EntityUid entity)
    {
        // No recursion for you
        var targetXform = Transform(entity);
        while (targetXform.ParentUid.IsValid())
        {
            if (targetXform.ParentUid == follower)
                return;

            targetXform = Transform(targetXform.ParentUid);
        }

        var followerComp = EnsureComp<FollowerComponent>(follower);
        followerComp.Following = entity;

        var followedComp = EnsureComp<FollowedComponent>(entity);

        if (!followedComp.Following.Add(follower))
            return;

        if (TryComp<JointComponent>(follower, out var joints))
            _jointSystem.ClearJoints(follower, joints);

        _physicsSystem.SetLinearVelocity(follower, Vector2.Zero);

        var xform = Transform(follower);
        _containerSystem.AttachParentToContainerOrGrid(xform);

        // If we didn't get to parent's container.
        if (xform.ParentUid != Transform(xform.ParentUid).ParentUid)
        {
            _transform.SetCoordinates(follower, xform, new EntityCoordinates(entity, Vector2.Zero), rotation: Angle.Zero);
        }

        EnsureComp<OrbitVisualsComponent>(follower);

        var followerEv = new StartedFollowingEntityEvent(entity, follower);
        var entityEv = new EntityStartedFollowingEvent(entity, follower);

        RaiseLocalEvent(follower, followerEv);
        RaiseLocalEvent(entity, entityEv);
        Dirty(followedComp);
    }

    /// <summary>
    ///     Forces an entity to stop following another entity, if it is doing so.
    /// </summary>
    /// <param name="deparent">Should the entity deparent itself</param>
    public void StopFollowingEntity(EntityUid uid, EntityUid target, FollowedComponent? followed = null, bool deparent = true)
    {
        if (!Resolve(target, ref followed, false))
            return;

        if (!HasComp<FollowerComponent>(uid))
            return;

        followed.Following.Remove(uid);
        if (followed.Following.Count == 0)
            RemComp<FollowedComponent>(target);

        RemComp<FollowerComponent>(uid);
        RemComp<OrbitVisualsComponent>(uid);
        var uidEv = new StoppedFollowingEntityEvent(target, uid);
        var targetEv = new EntityStoppedFollowingEvent(target, uid);

        RaiseLocalEvent(uid, uidEv, true);
        RaiseLocalEvent(target, targetEv, false);
        Dirty(target, followed);
        RaiseLocalEvent(uid, uidEv);
        RaiseLocalEvent(target, targetEv);

        if (!deparent || !TryComp(uid, out TransformComponent? xform))
            return;

        _transform.AttachToGridOrMap(uid, xform);
        if (xform.MapUid != null)
            return;

        if (_netMan.IsClient)
        {
            _transform.DetachParentToNull(uid, xform);
            return;
        }

        Log.Warning($"A follower has been detached to null-space and will be deleted. Follower: {ToPrettyString(uid)}. Followed: {ToPrettyString(target)}");
        QueueDel(uid);
    }

    /// <summary>
    ///     Forces all of an entity's followers to stop following it.
    /// </summary>
    public void StopAllFollowers(EntityUid uid,
        FollowedComponent? followed=null)
    {
        if (!Resolve(uid, ref followed))
            return;

        foreach (var player in followed.Following)
        {
            StopFollowingEntity(player, uid, followed);
        }
    }
}

public abstract class FollowEvent : EntityEventArgs
{
    public EntityUid Following;
    public EntityUid Follower;

    protected FollowEvent(EntityUid following, EntityUid follower)
    {
        Following = following;
        Follower = follower;
    }
}

/// <summary>
///     Raised on an entity when it start following another entity.
/// </summary>
public sealed class StartedFollowingEntityEvent : FollowEvent
{
    public StartedFollowingEntityEvent(EntityUid following, EntityUid follower) : base(following, follower)
    {
    }
}

/// <summary>
///     Raised on an entity when it stops following another entity.
/// </summary>
public sealed class StoppedFollowingEntityEvent : FollowEvent
{
    public StoppedFollowingEntityEvent(EntityUid following, EntityUid follower) : base(following, follower)
    {
    }
}

/// <summary>
///     Raised on an entity when it start following another entity.
/// </summary>
public sealed class EntityStartedFollowingEvent : FollowEvent
{
    public EntityStartedFollowingEvent(EntityUid following, EntityUid follower) : base(following, follower)
    {
    }
}

/// <summary>
///     Raised on an entity when it starts being followed by another entity.
/// </summary>
public sealed class EntityStoppedFollowingEvent : FollowEvent
{
    public EntityStoppedFollowingEvent(EntityUid following, EntityUid follower) : base(following, follower)
    {
    }
}
