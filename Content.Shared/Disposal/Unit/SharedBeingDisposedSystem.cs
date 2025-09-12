using Content.Shared.Disposal.Components;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// This system handles entities that are currently being flushed through
/// the disposals system. These entities should all have a temporary
/// <see cref="BeingDisposedComponent"/>.
/// </summary>
public abstract class SharedBeingDisposedSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposableSystem _disposable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, DisposalSystemTransitionEvent>(OnStartup);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BeingDisposedComponent, EntityStartedFollowingEvent>(OnStartedFollowing);
        SubscribeLocalEvent<BeingDisposedComponent, EntityStoppedFollowingEvent>(OnStoppedFollowing);
    }

    private void OnStartup(Entity<BeingDisposedComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        if (!TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        // Any followers of the entity will also be attached to its disposal holder.
        // This is so that movement under subfloors can be predicted by follower clients.
        foreach (var follower in followed.Following)
        {
            _disposable.AttachEntityToDisposalHolder((ent.Comp.Holder, holder), follower);
        }
    }

    private void OnShutdown(Entity<BeingDisposedComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        foreach (var follower in followed.Following)
        {
            _disposable.DetachEntityFromDisposalHolder(follower);
        }
    }

    private void OnStartedFollowing(Entity<BeingDisposedComponent> ent, ref EntityStartedFollowingEvent args)
    {
        if (!TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        _disposable.AttachEntityToDisposalHolder((ent.Comp.Holder, holder), args.Follower);
    }

    private void OnStoppedFollowing(Entity<BeingDisposedComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        _disposable.DetachEntityFromDisposalHolder(args.Follower);
    }
}
