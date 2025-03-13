using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Server-side logic for <see cref="SharedOccluderSignalControlSystem"/>.
/// </summary>
public sealed class OccluderSignalControlSystem : SharedOccluderSignalControlSystem
{
    [Dependency] private readonly ServerOccluderSystem _occluder = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OccluderSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
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
