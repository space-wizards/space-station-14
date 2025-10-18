using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;

namespace Content.Shared.Disposal.SignalRouter;

/// <summary>
/// This handles the emission of signals when entities in disposals
/// pass through a disposal tube with a <see cref="DisposalSignallerComponent"/>.
/// </summary>
public sealed class DisposalSignallerSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisposalSignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisposalSignallerComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(DisposalTubeSystem) });
    }

    private void OnInit(Entity<DisposalSignallerComponent> ent, ref ComponentInit args)
    {
        _link.EnsureSourcePorts(ent, ent.Comp.Port);
    }

    private void OnGetNextDirection(Entity<DisposalSignallerComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        _link.InvokePort(ent, ent.Comp.Port);
    }
}
