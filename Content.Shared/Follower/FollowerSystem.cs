using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Content.Shared.Follower.Components;
using Robust.Shared.Maths;

namespace Content.Shared.Follower;

public sealed class FollowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<FollowerComponent, RelayMoveInputEvent>(OnFollowerMove);
        SubscribeLocalEvent<FollowedComponent, EntityTerminatingEvent>(OnFollowedTerminating);
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

    // Since we parent our observer to the followed entity, we need to detach
    // before they get deleted so that we don't get recursively deleted too.
    private void OnFollowedTerminating(EntityUid uid, FollowedComponent component, EntityTerminatingEvent args)
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
        var followerComp = EnsureComp<FollowerComponent>(follower);
        followerComp.Following = entity;

        var followedComp = EnsureComp<FollowedComponent>(entity);
        followedComp.Following.Add(follower);

        var xform = Transform(follower);
        xform.AttachParent(entity);
        xform.LocalPosition = Vector2.Zero;
    }

    /// <summary>
    ///     Forces an entity to stop following another entity, if it is doing so.
    /// </summary>
    public void StopFollowingEntity(EntityUid uid, EntityUid target,
        FollowedComponent? followed=null)
    {
        if (!Resolve(target, ref followed))
            return;

        if (!HasComp<FollowerComponent>(uid))
            return;

        followed.Following.Remove(uid);
        if (followed.Following.Count == 0)
            RemComp<FollowedComponent>(target);
        RemComp<FollowerComponent>(uid);
        Transform(uid).AttachToGridOrMap();
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
