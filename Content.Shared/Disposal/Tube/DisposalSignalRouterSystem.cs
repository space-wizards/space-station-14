using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Handles signals and the routing get next direction event.
/// </summary>
public sealed class DisposalSignalRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalSignalRouterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisposalSignalRouterComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<DisposalSignalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(SharedDisposalTubeSystem) });
    }

    private void OnInit(EntityUid uid, DisposalSignalRouterComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.OnPort, comp.OffPort, comp.TogglePort);
    }

    private void OnSignalReceived(EntityUid uid, DisposalSignalRouterComponent comp, ref SignalReceivedEvent args)
    {
        // TogglePort flips it
        // OnPort sets it to true
        // OffPort sets it to false
        comp.Routing = args.Port == comp.TogglePort
            ? !comp.Routing
            : args.Port == comp.OnPort;

        Dirty(uid, comp);
    }

    private void OnGetNextDirection(EntityUid uid, DisposalSignalRouterComponent comp, ref GetDisposalsNextDirectionEvent args)
    {
        if (!comp.Routing)
        {
            args.Next = Transform(uid).LocalRotation.GetDir();
            return;
        }

        // use the junction side direction when a tag matches
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);
        args.Next = ev.Connectable[1];
    }
}
