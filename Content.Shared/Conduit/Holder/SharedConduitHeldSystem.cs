using Content.Shared.Disposal.Components;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Conduit.Holder;

/// <summary>
/// This system handles entities that are currently being transported through a conduit system.
/// These entities have a temporary <see cref="ConduitHeldComponent"/>.
/// </summary>
public abstract class SharedConduitHeldSystem : EntitySystem
{
    [Dependency] private readonly SharedConduitHolderSystem _conduitHolder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitHeldComponent, DisposalSystemTransitionEvent>(OnStartup);
        SubscribeLocalEvent<ConduitHeldComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ConduitHeldComponent, EntityStartedFollowingEvent>(OnStartedFollowing);
        SubscribeLocalEvent<ConduitHeldComponent, EntityStoppedFollowingEvent>(OnStoppedFollowing);

        SubscribeLocalEvent<ConduitHeldComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ConduitHeldComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnStartup(Entity<ConduitHeldComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        if (!TryComp<ConduitHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        // Any followers of the entity will also be attached to its conduit holder.
        // This is so that movement under subfloors can be predicted by follower clients.
        foreach (var follower in followed.Following)
        {
            _conduitHolder.AttachEntityToConduitHolder((ent.Comp.Holder, holder), follower);
        }
    }

    private void OnShutdown(Entity<ConduitHeldComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

        foreach (var follower in followed.Following)
        {
            _conduitHolder.DetachEntityFromConduitHolder(follower);
        }
    }

    private void OnStartedFollowing(Entity<ConduitHeldComponent> ent, ref EntityStartedFollowingEvent args)
    {
        if (!TryComp<ConduitHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        _conduitHolder.AttachEntityToConduitHolder((ent.Comp.Holder, holder), args.Follower);
    }

    private void OnStoppedFollowing(Entity<ConduitHeldComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        _conduitHolder.DetachEntityFromConduitHolder(args.Follower);
    }

    private void OnInteractionAttempt(Entity<ConduitHeldComponent> ent, ref InteractionAttemptEvent args)
    {
        // Prevent interactions while travelling through conduits
        args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<ConduitHeldComponent> ent, ref AttackAttemptEvent args)
    {
        // Prevent attacking while travelling through conduits
        args.Cancel();
    }
}
