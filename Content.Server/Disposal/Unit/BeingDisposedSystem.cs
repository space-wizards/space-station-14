using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;

namespace Content.Server.Disposal.Unit;

public sealed class BeingDisposedSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposableSystem _disposable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, DisposalSystemTransitionEvent>(OnStartup);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BeingDisposedComponent, EntityStartedFollowingEvent>(OnStartedFollowing);
        SubscribeLocalEvent<BeingDisposedComponent, EntityStoppedFollowingEvent>(OnStoppedFollowing);

        SubscribeLocalEvent<BeingDisposedComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }


    private void OnStartup(Entity<BeingDisposedComponent> ent, ref DisposalSystemTransitionEvent args)
    {
        if (!TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
            return;

        if (!TryComp<FollowedComponent>(ent, out var followed))
            return;

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


    private void OnGetAir(EntityUid uid, BeingDisposedComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
            args.Handled = true;
        }
    }

    private void OnInhaleLocation(EntityUid uid, BeingDisposedComponent component, InhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }

    private void OnExhaleLocation(EntityUid uid, BeingDisposedComponent component, ExhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }
}
