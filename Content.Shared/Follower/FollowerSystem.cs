using Content.Shared.Database;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Shared.Follower;

public sealed class FollowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<FollowerComponent, RelayMoveInputEvent>(OnFollowerMove);
        SubscribeLocalEvent<FollowedComponent, ComponentRemove>(OnFollowedRemoved);

        SubscribeLocalEvent<FollowerComponent, ComponentGetState>(OnFollowerGetState);
        SubscribeLocalEvent<FollowerComponent, ComponentHandleState>(OnFollowerHandleState);
        SubscribeLocalEvent<FollowedComponent, ComponentGetState>(OnFollowedGetState);
        SubscribeLocalEvent<FollowedComponent, ComponentHandleState>(OnFollowedHandleState);
    }

    private void OnGetAlternativeVerbs(GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!HasComp<SharedGhostComponent>(ev.User))
            return;

        if (ev.User == ev.Target)
            return;

        var verb = new AlternativeVerb
        {
            Priority = 10,
            Act = (() =>
            {
                StartFollowingEntity(ev.User, ev.Target);
            }),
            Impact = LogImpact.Low,
            Text = Loc.GetString("verb-follow-text"),
            IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png",
        };

        ev.Verbs.Add(verb);
    }

    private void OnFollowerMove(EntityUid uid, FollowerComponent component, RelayMoveInputEvent args)
    {
        StopFollowingEntity(uid, component.Following);
    }

    /// <summary>
    ///     Detach all followers if the followed entity is deleted.
    /// </summary>
    private void OnFollowedRemoved(EntityUid uid, FollowedComponent component, ComponentRemove args)
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
        if (Transform(entity).ParentUid == follower)
            return;

        var followerComp = EnsureComp<FollowerComponent>(follower);
        followerComp.Following = entity;
        Dirty(followerComp);

        var followedComp = EnsureComp<FollowedComponent>(entity);
        followedComp.Followers.Add(follower);
        Dirty(followedComp);

        var xform = Transform(follower);
        xform.LocalRotation = Angle.Zero;

        EnsureComp<OrbitVisualsComponent>(follower);

        var followerEv = new StartedFollowingEntityEvent(entity, follower);
        var entityEv = new EntityStartedFollowingEvent(entity, follower);

        RaiseLocalEvent(follower, followerEv);
        RaiseLocalEvent(entity, entityEv, false);
    }

    /// <summary>
    ///     Forces an entity to stop following another entity, if it is doing so.
    /// </summary>
    public void StopFollowingEntity(EntityUid uid, EntityUid target,
        FollowedComponent? followed=null)
    {
        if (!Resolve(target, ref followed, false))
            return;

        if (!HasComp<FollowerComponent>(uid))
            return;

        followed.Followers.Remove(uid);
        if (followed.Followers.Count == 0)
            RemComp<FollowedComponent>(target);
        RemComp<FollowerComponent>(uid);

        Transform(uid).AttachToGridOrMap();

        RemComp<OrbitVisualsComponent>(uid);

        var uidEv = new StoppedFollowingEntityEvent(target, uid);
        var targetEv = new EntityStoppedFollowingEvent(target, uid);

        RaiseLocalEvent(uid, uidEv);
        RaiseLocalEvent(target, targetEv, false);
    }

    /// <summary>
    ///     Forces all of an entity's followers to stop following it.
    /// </summary>
    public void StopAllFollowers(EntityUid uid,
        FollowedComponent? followed=null)
    {
        if (!Resolve(uid, ref followed))
            return;

        foreach (var player in followed.Followers)
        {
            StopFollowingEntity(player, uid, followed);
        }
    }

    private void OnFollowerGetState(EntityUid uid, FollowerComponent component, ref ComponentGetState args)
    {
        args.State = new FollowerComponentState(component.Following);
    }

    private void OnFollowerHandleState(EntityUid uid, FollowerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is FollowerComponentState state)
            component.Following = state.Following;
    }

    private void OnFollowedGetState(EntityUid uid, FollowedComponent component, ref ComponentGetState args)
    {
        args.State = new FollowedComponentState(component.Followers);
    }

    private void OnFollowedHandleState(EntityUid uid, FollowedComponent component, ref ComponentHandleState args)
    {
        if (args.Current is FollowedComponentState state)
            component.Followers = state.Followers;
    }
}

[PublicAPI]
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
