using Content.Shared.Administration.Logs;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    [Dependency] private EntityQuery<DeviceLinkSinkComponent> _deviceLinkSinkQuery;
    [Dependency] private EntityQuery<DeviceLinkSourceComponent> _deviceLinkSourceQuery;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery;

    [SubscribeLocalEvent]
    private void OnGetState(Entity<DeviceLinkSourceComponent> ent, ref ComponentGetState args)
    {
        var netOutputs = new Dictionary<ProtoId<SourcePortPrototype>, HashSet<NetEntity>>(ent.Comp.Outputs.Count);
        foreach (var (key, output) in ent.Comp.Outputs)
        {
            var set = GetNetEntitySet(output);
            netOutputs.Add(key, set);
        }

        var linkedPorts = new Dictionary<NetEntity, HashSet<DeviceLink>>(ent.Comp.LinkedPorts.Count);
        foreach (var (sink, links) in ent.Comp.LinkedPorts)
        {
            linkedPorts.Add(GetNetEntity(sink), [.. links]);
        }

        args.State = new DeviceLinkSourceComponentState(
            netOutputs,
            new Dictionary<ProtoId<SourcePortPrototype>, bool>(ent.Comp.LastSignals),
            linkedPorts,
            [.. ent.Comp.Ports]);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<DeviceLinkSourceComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DeviceLinkSourceComponentState state)
            return;

        var outputs = new Dictionary<ProtoId<SourcePortPrototype>, HashSet<EntityUid>>(state.Outputs.Count);
        foreach (var (key, output) in state.Outputs)
        {
            var netSet = EnsureEntitySet<DeviceLinkSinkComponent>(output, ent.Owner);
            var set = new HashSet<EntityUid>(netSet.Count);
            foreach (var uid in netSet)
            {
                if (Exists(uid) && !TerminatingOrDeleted(uid))
                    set.Add(uid);
            }

            outputs.Add(key, set);
        }

        var linked = new Dictionary<EntityUid, HashSet<DeviceLink>>(state.LinkedPorts.Count);
        // Prediction must never mutate the cached state used for the next rollback.
        foreach (var (net, value) in state.LinkedPorts)
        {
            var uid = EnsureEntity<DeviceLinkSinkComponent>(net, ent.Owner);
            if (uid.IsValid())
                linked.Add(uid, [.. value]);
        }

        ent.Comp.Outputs = outputs;
        ent.Comp.LinkedPorts = linked;
        ent.Comp.LastSignals = new Dictionary<ProtoId<SourcePortPrototype>, bool>(state.LastSignals);
        ent.Comp.Ports = [.. state.Ports];
    }

    /// <summary>
    /// Removes invalid links where the saved sink doesn't exist/have a sink component for example
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSourceStartup(Entity<DeviceLinkSourceComponent> source, ref ComponentStartup args)
    {
        ValueList<EntityUid> invalidSinks = new(source.Comp.LinkedPorts.Count);
        ValueList<DeviceLink> invalidLinks = new(source.Comp.LinkedPorts.Count);
        foreach (var (sink, links)  in source.Comp.LinkedPorts)
        {
            if (!_deviceLinkSinkQuery.TryComp(sink, out var sinkComponent))
            {
                invalidSinks.Add(sink);
                continue;
            }

            foreach (var link in links)
            {
                if (sinkComponent.Ports.Contains(link.Sink) && source.Comp.Ports.Contains(link.Source))
                    source.Comp.Outputs.GetOrNew(link.Source).Add(sink);
                else
                    invalidLinks.Add(link);
            }

            foreach (var link in invalidLinks)
            {
                Log.Warning($"Device source {ToPrettyString(source)} contains invalid links to entity {ToPrettyString(sink)}: {link.Source}->{link.Sink}");
                links.Remove(link);
            }

            if (links.Count == 0)
            {
                invalidSinks.Add(sink);
                continue;
            }

            invalidLinks.Clear();
            sinkComponent.LinkedSources.Add(source.Owner);
            DirtyField(sink, sinkComponent, nameof(DeviceLinkSinkComponent.LinkedSources));
        }

        foreach (var sink in invalidSinks)
        {
            source.Comp.LinkedPorts.Remove(sink);
            Log.Warning($"Device source {ToPrettyString(source)} contains invalid sink: {ToPrettyString(sink)}");
        }

        Dirty(source);
    }

    /// <summary>
    /// Ensures that its links get deleted when a source gets removed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSourceRemoved(Entity<DeviceLinkSourceComponent> source, ref ComponentRemove args)
    {
        foreach (var sinkUid in source.Comp.LinkedPorts.Keys)
        {
            if (_deviceLinkSinkQuery.TryGetComponent(sinkUid, out var sink))
                RemoveSinkFromSourceInternal(source, (sinkUid, sink));
            else
                Log.Error($"Device source {ToPrettyString(source)} links to invalid entity: {ToPrettyString(sinkUid)}");
        }
    }

    /// <summary>
    /// Ensures that its links get deleted when a sink gets removed
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSinkRemoved(Entity<DeviceLinkSinkComponent> sink, ref ComponentRemove args)
    {
        foreach (var sourceUid in sink.Comp.LinkedSources)
        {
            if (_deviceLinkSourceQuery.TryComp(sourceUid, out var source))
                RemoveSinkFromSourceInternal((sourceUid, source), sink);
            else
                Log.Error($"Device sink {ToPrettyString(sink)} source list contains invalid entity: {ToPrettyString(sourceUid)}");
        }
    }

    /// <summary>
    /// Gets how many times a <see cref="DeviceLinkSinkComponent"/> has been invoked recently.
    /// </summary>
    /// <remarks>
    /// The return value of this function goes up by one every time a sink is invoked, and goes down by one every tick.
    /// </remarks>
    [PublicAPI]
    public int GetEffectiveInvokeCounter(DeviceLinkSinkComponent sink)
    {
        // Shouldn't be possible but just to be safe.
        var curTick = _gameTiming.CurTick;
        if (curTick < sink.InvokeCounterTick)
            return 0;

        var tickDelta = curTick.Value - sink.InvokeCounterTick.Value;
        if (tickDelta >= sink.InvokeCounter)
            return 0;

        return Math.Max(0, sink.InvokeCounter - (int)tickDelta);
    }

    /// <summary>
    /// Sets <see cref="DeviceLinkSinkComponent.InvokeLimit"/> to a new value and dirties it.
    /// </summary>
    [PublicAPI]
    public void SetInvokeLimit(Entity<DeviceLinkSinkComponent> sink, int value)
    {
        sink.Comp.InvokeLimit = value;
        DirtyField(sink.AsNullable(), nameof(DeviceLinkSinkComponent.InvokeLimit));
    }

    private void SetInvokeCounter(Entity<DeviceLinkSinkComponent> sink, int value)
    {
        sink.Comp.InvokeCounterTick = _gameTiming.CurTick;
        sink.Comp.InvokeCounter = value;
        DirtyFields(sink.AsNullable(), null, nameof(DeviceLinkSinkComponent.InvokeCounterTick), nameof(DeviceLinkSinkComponent.InvokeCounter));
    }
}
