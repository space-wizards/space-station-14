using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Holder;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Disposal.Unit;

/// <summary>
/// This system handles entities that are currently being flushed through
/// the disposals system. These entities should all have a temporary
/// <see cref="BeingDisposedComponent"/>.
/// </summary>
public abstract class SharedBeingDisposedSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposalHolderSystem _disposalHolder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, DisposalSystemTransitionEvent>(OnTransition);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BeingDisposedComponent, EntityStartedFollowingEvent>(OnStartedFollowing);
        SubscribeLocalEvent<BeingDisposedComponent, EntityStoppedFollowingEvent>(OnStoppedFollowing);

        SubscribeLocalEvent<BeingDisposedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<BeingDisposedComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnTransition(Entity<BeingDisposedComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        if (!TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        // Any followers of the entity will also be attached to its disposal holder.
        // This is so that movement under subfloors can be predicted by follower clients.
        foreach (var follower in followed.Following)
        {
            _disposalHolder.AttachEntity((ent.Comp.Holder, holder), follower);
        }
    }

    private void OnShutdown(Entity<BeingDisposedComponent> ent, ref ComponentShutdown args)
    {
        // Remove followers from the disposal holder
        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        foreach (var follower in followed.Following)
        {
            _disposalHolder.DetachEntity(follower);
        }
    }

    private void OnStartedFollowing(Entity<BeingDisposedComponent> ent, ref EntityStartedFollowingEvent args)
    {
        // Attach new followers to the disposal holder to prevent mispredicts
        if (!TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        _disposalHolder.AttachEntity((ent.Comp.Holder, holder), args.Follower);
    }

    private void OnStoppedFollowing(Entity<BeingDisposedComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        // Remove departing followers from the disposal holder
        _disposalHolder.DetachEntity(args.Follower);
    }

    private void OnInteractionAttempt(Entity<BeingDisposedComponent> ent, ref InteractionAttemptEvent args)
    {
        // Prevent interactions while travelling through disposals
        args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<BeingDisposedComponent> ent, ref AttackAttemptEvent args)
    {
        // Prevent attacking while travelling through disposals
        args.Cancel();
    }
}
