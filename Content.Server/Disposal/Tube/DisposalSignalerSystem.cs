using Content.Shared.DeviceLinking;
using Content.Server.DeviceLinking.Systems;

namespace Content.Server.Disposal.Tube;

/// <summary>
/// Handles signals.
/// </summary>
public sealed class DisposalSignalerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalSignalerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisposalSignalerComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(DisposalTubeSystem) });

    }

    private void OnInit(EntityUid uid, DisposalSignalerComponent comp, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, comp.Port);
    }
    private void OnGetNextDirection(EntityUid uid, DisposalSignalerComponent comp, ref GetDisposalsNextDirectionEvent args)
    {
        _link.InvokePort(uid, comp.Port);
    }
}
