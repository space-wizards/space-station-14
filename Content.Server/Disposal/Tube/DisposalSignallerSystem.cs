using Content.Server.DeviceLinking.Systems;

namespace Content.Server.Disposal.Tube;

public sealed class DisposalSignallerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisposalSignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisposalSignallerComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(DisposalTubeSystem) });
    }

    private void OnInit(EntityUid uid, DisposalSignallerComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, comp.Port);
    }

    private void OnGetNextDirection(EntityUid uid, DisposalSignallerComponent comp, ref GetDisposalsNextDirectionEvent args)
    {
        _link.InvokePort(uid, comp.Port);
    }
}
