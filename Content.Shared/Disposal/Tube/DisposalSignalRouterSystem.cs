using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using System.Linq;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Handles signals and the routing get next direction event.
/// </summary>
public sealed class DisposalSignalRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedDisposalTubeSystem _disposalTube = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalSignalRouterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisposalSignalRouterComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<DisposalSignalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(SharedDisposalTubeSystem) });
    }

    private void OnInit(Entity<DisposalSignalRouterComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort, ent.Comp.OffPort, ent.Comp.TogglePort);
    }

    private void OnSignalReceived(Entity<DisposalSignalRouterComponent> ent, ref SignalReceivedEvent args)
    {
        // TogglePort flips it
        // OnPort sets it to true
        // OffPort sets it to false
        ent.Comp.Routing = args.Port == ent.Comp.TogglePort
            ? !ent.Comp.Routing
            : args.Port == ent.Comp.OnPort;

        Dirty(ent);
    }

    private void OnGetNextDirection(Entity<DisposalSignalRouterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var exits = _disposalTube.GetTubeConnectableDirections((ent, ent.Comp));

        if (exits.Length < 3 || !ent.Comp.Routing)
        {
            _disposalTube.SelectNextTube((ent, ent.Comp), exits, ref args);
            return;
        }

        _disposalTube.SelectNextTube((ent, ent.Comp), exits.Skip(1).ToArray(), ref args);
    }
}
