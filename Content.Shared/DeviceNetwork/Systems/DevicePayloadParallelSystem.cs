using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Threading;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// A version of <see cref="DevicePayloadSystem{T}"/> that raises the network payload in multiple threads.
/// This is usually good for broadcast payloads in which the order of receiving doesn't matter, for example 'Ping' requests.
/// </summary>
/// <remarks>
/// EXPERIMENTAL! Please use this with care and don't access any ECS methods while processing the code in parallel.
/// </remarks>
/// <typeparam name="T">Component that is being handled.</typeparam>
public abstract partial class DevicePayloadParallelSystem<T> : DeviceNetworkHandler, IEntityDeviceNetworkHandler, IParallelDeviceNetworkHandler where T : IComponent
{
    [Dependency] protected IParallelManager ParallelManager = default!;
    [Dependency] protected EntityQuery<T> Query = default!;

    public FrozenDictionary<Type, Delegate> PayloadSubs { get; protected set; } = default!;
    protected readonly Dictionary<Type, Delegate> PayloadSubsCache = new();

    public FrozenDictionary<Type, Delegate> ParallelPayloadSubs { get; protected set; } = default!;
    protected readonly Dictionary<Type, Delegate> ParallelPayloadSubsCache = new();

    private DeviceNetworkPacketJob<T> _job;

    protected override void Register()
    {
        foreach (var payload in PayloadSubs.Keys)
        {
            DeviceSystem.HandlersCache.Add(payload, this);
        }

        foreach (var payload in ParallelPayloadSubs.Keys)
        {
            DeviceSystem.ParallelHandlersCache.Add(payload, this);
        }
    }

    protected virtual int BatchSize => 1; // TODO fine-tune this value

    protected virtual int MinBatchSize => 1;

    public override void Initialize()
    {
        base.Initialize();
        InitializeDevice();
        PayloadSubs = PayloadSubsCache.ToFrozenDictionary();
        ParallelPayloadSubs = ParallelPayloadSubsCache.ToFrozenDictionary();
        Register();
        _job = new DeviceNetworkPacketJob<T>(BatchSize, MinBatchSize);
    }

    [UsedImplicitly]
    protected void SubscribePayload<TN>(DeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (IsInitialized)
        {
            Log.Error($"Tried to register a device network payload handler in type {typeof(TN).Name} after initialize!");
            return;
        }

        // It needs to be wrapped so when raising the Delegate it can be down-casted without issues.
        DeviceNetworkPayloadHandlerWrapper<T> wrapper = (ent, ref basePayload, ref args) =>
        {
            var specificPayload = (TN) basePayload;
            handler(ent, ref specificPayload, ref args);
            basePayload = specificPayload;
        };

        PayloadSubsCache.Add(typeof(TN), wrapper);
    }

    public void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!PayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        if (!Query.TryComp(uid, out var comp))
            return;

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        del.Invoke((uid, comp), ref payload, ref args);
    }

    [UsedImplicitly]
    protected void SubscribePayloadParallel<TN>(DeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (IsInitialized)
        {
            Log.Error($"Tried to register a device network payload handler in type {typeof(TN).Name} after initialize!");
            return;
        }

        // It needs to be wrapped so when raising the Delegate it can be down-casted without issues.
        DeviceNetworkPayloadHandlerWrapper<T> wrapper = (ent, ref basePayload, ref args) =>
        {
            var specificPayload = (TN) basePayload;
            handler(ent, ref specificPayload, ref args);
            basePayload = specificPayload;
        };

        ParallelPayloadSubsCache.Add(typeof(TN), wrapper);
    }

    public void RaisePayloadParallel(ReadOnlySpan<Device> devices, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!ParallelPayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        var ents = new ValueList<Entity<T>>(devices.Length);
        foreach (var device in devices)
        {
            if (!Query.TryComp(device.DeviceOwner, out var comp))
                continue;

            ents.Add((device.DeviceOwner, comp));
        }

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        _job.Delegate = del;
        _job.Targets = ents.ToArray();
        _job.Payload = payload;
        _job.PacketData = args;

        ParallelManager.ProcessNow(_job, _job.Targets.Length);
    }

    private record struct DeviceNetworkPacketJob<TC> : IParallelRobustJob where TC : IComponent
    {
        public DeviceNetworkPayloadHandlerWrapper<TC> Delegate = default!;

        public Entity<TC>[] Targets = default!;

        public HandledNetworkPayload Payload = default!;

        public DeviceNetworkPacketData PacketData = default!;

        public int BatchSize { get; }

        public int MinimumBatchParallel { get; }

        public DeviceNetworkPacketJob(int batchSize, int minBatchParallel)
        {
            BatchSize = batchSize;
            MinimumBatchParallel = minBatchParallel;
        }

        public void Execute(int index)
        {
            Delegate.Invoke(Targets[index], ref Payload, ref PacketData);
        }
    }
}
