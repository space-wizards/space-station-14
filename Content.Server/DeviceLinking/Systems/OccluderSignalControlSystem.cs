using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Robust.Server.GameObjects;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Controls an <see cref="OccluderComponent"/> through device signals.
/// <seealso cref="OccluderSignalControlComponent"/>
/// </summary>
public sealed class OccluderSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly ServerOccluderSystem _occluder = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OccluderSignalControlComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OccluderSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<OccluderSignalControlComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.EnablePort, ent.Comp.DisablePort, ent.Comp.TogglePort);
    }

    private void OnSignalReceived(Entity<OccluderSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        // Early return if there's nothing to toggle.
        if (!TryComp<OccluderComponent>(ent, out var occluder))
            return;

        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        // Only process signals that are High or Momentary
        if (state != SignalState.High && state != SignalState.Momentary)
            return;

        if (args.Port == ent.Comp.EnablePort)
        {
            _occluder.SetEnabled(ent, true, occluder);
        }
        else if (args.Port == ent.Comp.DisablePort)
        {
            _occluder.SetEnabled(ent, false, occluder);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            _occluder.SetEnabled(ent, !occluder.Enabled, occluder);
        }
    }
}
