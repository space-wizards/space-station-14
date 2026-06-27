using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Threading;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// A delegate for events that are ran before the parallel processing of <see cref="DevicePayloadParallelSystem{T}"/>.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
/// <typeparam name="TN">Type of the network payload.</typeparam>
public delegate void BeforeDeviceNetworkPayloadHandler<TC, TN>(Entity<TC> ent, ref TN payload, ref DeviceNetworkPacketData args)
    where TC : IComponent
    where TN : HandledNetworkPayload;

/// <summary>
/// A delegate for events that are ran after the parallel processing of <see cref="DevicePayloadParallelSystem{T}"/>.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
/// <typeparam name="TN">Type of the network payload.</typeparam>
public delegate void AfterDeviceNetworkPayloadHandler<TC, TN>(Entity<TC> ent, ref TN payload, ref DeviceNetworkPacketData args)
    where TC : IComponent
    where TN : HandledNetworkPayload;

/// <summary>
/// A wrapper for the <see cref="BeforeDeviceNetworkPayloadHandler{TC, TN}"/> delegate.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
public delegate void BeforeParallelDeviceNetworkPayloadHandlerWrapper<TC>(
    Entity<TC> ent,
    ref HandledNetworkPayload payload,
    ref DeviceNetworkPacketData args)
    where TC : IComponent;

/// <summary>
/// A wrapper for the <see cref="AfterDeviceNetworkPayloadHandler{TC, TN}"/> delegate.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
public delegate void AfterDeviceNetworkPayloadHandlerWrapper<TC>(
    Entity<TC> ent,
    ref HandledNetworkPayload payload,
    ref DeviceNetworkPacketData args)
    where TC : IComponent;

/// <summary>
/// A version of <see cref="DevicePayloadSystem{T}"/> that raises the network payload in multiple threads.
/// This is usually good for broadcast payloads in which the order of receiving doesn't matter, for example 'Ping' requests.
/// </summary>
/// <remarks>
/// EXPERIMENTAL! Please use this with care and don't access any ECS methods while processing the code in parallel.
/// </remarks>
/// <typeparam name="T">Component that is being handled.</typeparam>
public abstract partial class DevicePayloadParallelSystem<T> : DevicePayloadSystem<T>, IParallelDeviceNetworkHandler where T : IComponent
{
    [Dependency] protected IParallelManager ParallelManager = default!;

    public FrozenDictionary<Type, Delegate> BeforePayloadSubs { get; protected set; } = default!;
    public FrozenDictionary<Type, Delegate> ParallelPayloadSubs { get; protected set; } = default!;
    public FrozenDictionary<Type, Delegate> AfterPayloadSubs { get; protected set; } = default!;

    protected readonly Dictionary<Type, Delegate> BeforePayloadSubsCache = new();
    protected readonly Dictionary<Type, Delegate> ParallelPayloadSubsCache = new();
    protected readonly Dictionary<Type, Delegate> AfterPayloadSubsCache = new();

    protected virtual int BatchSize => 1;

    protected virtual int MinBatchSize => 1;

    private DeviceNetworkPacketJob<T> _job;

    protected override void Register()
    {
        base.Register();
        foreach (var payload in ParallelPayloadSubs.Keys)
        {
            if (DeviceSystem.ParallelHandlersCache.TryAdd(payload, this))
                continue;

            Log.Error($"Duplicate payload subscription for payload {payload.Name}");
            return;
        }
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        _job = new DeviceNetworkPacketJob<T>(BatchSize, MinBatchSize);
    }

    protected override void LockSubscriptions()
    {
        base.LockSubscriptions();
        BeforePayloadSubs = BeforePayloadSubsCache.ToFrozenDictionary();
        ParallelPayloadSubs = ParallelPayloadSubsCache.ToFrozenDictionary();
        AfterPayloadSubs = AfterPayloadSubsCache.ToFrozenDictionary();
    }

    [UsedImplicitly]
    protected void SubscribeBeforeParallelPayload<TN>(BeforeDeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (DeviceInitialized)
        {
            Log.Error($"Tried to register a device network payload handler in type {typeof(TN).Name} after initialize!");
            return;
        }

        // It needs to be wrapped so when raising the Delegate it can be down-casted without issues.
        BeforeParallelDeviceNetworkPayloadHandlerWrapper<T> wrapper = (ent, ref basePayload, ref args) =>
        {
            var specificPayload = (TN) basePayload;
            handler(ent, ref specificPayload, ref args);
            basePayload = specificPayload;
        };

        if (BeforePayloadSubsCache.TryAdd(typeof(TN), wrapper))
            return;

        Log.Error($"Duplicate payload subscription for payload {typeof(TN).Name}");
    }

    [UsedImplicitly]
    protected void SubscribePayloadParallel<TN>(DeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (DeviceInitialized)
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

        if (ParallelPayloadSubsCache.TryAdd(typeof(TN), wrapper))
            return;

        Log.Error($"Duplicate payload subscription for payload {typeof(TN).Name}");
    }

    [UsedImplicitly]
    protected void SubscribeAfterParallelPayload<TN>(AfterDeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (DeviceInitialized)
        {
            Log.Error($"Tried to register a device network payload handler in type {typeof(TN).Name} after initialize!");
            return;
        }

        // It needs to be wrapped so when raising the Delegate it can be down-casted without issues.
        AfterDeviceNetworkPayloadHandlerWrapper<T> wrapper = (ent, ref basePayload, ref args) =>
        {
            var specificPayload = (TN) basePayload;
            handler(ent, ref specificPayload, ref args);
            basePayload = specificPayload;
        };

        if (AfterPayloadSubsCache.TryAdd(typeof(TN), wrapper))
            return;

        Log.Error($"Duplicate payload subscription for payload {typeof(TN).Name}");
    }

    /// <summary>
    /// Raises a <see cref="HandledNetworkPayload"/> on a list of multiple entities in parallel.
    /// </summary>
    /// <param name="devices">The target entities to raise the payload on.</param>
    /// <param name="payload">The payload to raise.</param>
    /// <param name="args">Other data about how the network packet was received.</param>
    public void RaisePayloadParallel(ReadOnlySpan<EntityUid?> devices, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!ParallelPayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        var ents = new ValueList<Entity<T>>(devices.Length); // TODO try out using an array pool here instead
        foreach (var device in devices)
        {
            if (!Query.TryComp(device, out var comp))
                continue;

            ents.Add((device.Value, comp));
        }

        if (BeforePayloadSubs.TryGetValue(payload.GetType(), out var beforePayload))
        {
            var beforeDel = (BeforeParallelDeviceNetworkPayloadHandlerWrapper<T>) beforePayload;
            foreach (var ent in ents)
            {
                beforeDel.Invoke(ent, ref payload, ref args);
            }
        }

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        _job.Delegate = del;
        _job.Targets = ents.ToArray();
        _job.Payload = payload;
        _job.PacketData = args;

        ParallelManager.ProcessNow(_job, _job.Targets.Length);

        if (AfterPayloadSubs.TryGetValue(payload.GetType(), out var afterPayload))
        {
            var afterDel = (AfterDeviceNetworkPayloadHandlerWrapper<T>) afterPayload;
            foreach (var ent in ents)
            {
                afterDel.Invoke(ent, ref payload, ref args);
            }
        }
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
