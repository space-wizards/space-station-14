using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using System.Linq;

namespace Content.Shared.Conduit.SignalRouter;

/// <summary>
/// Handles signal-based routing for entities in the disposal system.
/// </summary>
public sealed class ConduitSignalRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedConduitSystem _conduit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitSignalRouterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ConduitSignalRouterComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ConduitSignalRouterComponent, GetConduitNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(SharedConduitSystem) });
    }

    private void OnInit(Entity<ConduitSignalRouterComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort, ent.Comp.OffPort, ent.Comp.TogglePort);
    }

    private void OnSignalReceived(Entity<ConduitSignalRouterComponent> ent, ref SignalReceivedEvent args)
    {
        // TogglePort flips it
        // OnPort sets it to true
        // OffPort sets it to false

        ent.Comp.Routing = args.Port == ent.Comp.TogglePort
            ? !ent.Comp.Routing
            : args.Port == ent.Comp.OnPort;

        Dirty(ent);
    }

    private void OnGetNextDirection(Entity<ConduitSignalRouterComponent> ent, ref GetConduitNextDirectionEvent args)
    {
        if (!TryComp<ConduitComponent>(ent, out var conduit))
            return;

        var exits = _conduit.GetConnectableDirections((ent, conduit));

        if (exits.Length < 3 || !ent.Comp.Routing)
        {
            _conduit.SelectNextExit((ent, conduit), exits, ref args);
            return;
        }

        _conduit.SelectNextExit((ent, conduit), exits.Skip(1).ToArray(), ref args);
    }
}
