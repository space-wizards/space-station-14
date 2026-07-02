using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract partial class SharedDeviceNetworkSystem
{
    private FrozenDictionary<Type, IEntityDeviceNetworkHandler> _handlers = default!;
    private FrozenDictionary<Type, IBeforeDeviceNetworkHandler> _beforeHandlers = default!;
    private FrozenDictionary<Type, IParallelDeviceNetworkHandler> _parallelHandlers = default!;

    public readonly Dictionary<Type, IEntityDeviceNetworkHandler> HandlersCache = new();
    public readonly Dictionary<Type, IBeforeDeviceNetworkHandler> BeforeHandlersCache = new();
    public readonly Dictionary<Type, IParallelDeviceNetworkHandler> ParallelHandlersCache = new();

    /// <summary>
    /// Must be called after all systems were initialized.
    /// TODO ENGINE implement EntitySystem.PostInit()
    /// </summary>
    public void PostInit()
    {
        _handlers = HandlersCache.ToFrozenDictionary();
        _beforeHandlers = BeforeHandlersCache.ToFrozenDictionary();
        _parallelHandlers = ParallelHandlersCache.ToFrozenDictionary();
    }

    /// <summary>
    /// Raises a <see cref="NetworkPayload"/> to all subscribed <see cref="DeviceNetworkHandler"/>s.
    /// </summary>
    /// <param name="uid">UID of the entity that received the payload.</param>
    /// <param name="payload">The payload to raise.</param>
    /// <param name="args"></param>
    public void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!_handlers.TryGetValue(payload.GetType(), out var handler))
            return;

        handler.RaisePayload(uid, ref payload, ref args);
    }

    /// <summary>
    /// Runs network condition checks on an entity to determine if the payload should be sent or not.
    /// </summary>
    /// <param name="uid">UID of the entity that is receiving the payload.</param>
    /// <param name="args">Info about how the payload have been received.</param>
    public void RaiseBeforePayload(EntityUid uid, ref BeforePacketSentEvent args)
    {
        foreach (var (compType, handler) in _beforeHandlers)
        {
            if (!EntityManager.TryGetComponent(uid, compType, out var comp))
                continue;

            handler.RaiseBeforePayload(uid, comp, ref args);
        }
    }

    /// <summary>
    /// Raises a network payload on a span of devices in parallel.
    /// </summary>
    /// <param name="devices">Target entities to send the payload to.</param>
    /// <param name="payload">Payload to send.</param>
    /// <param name="args">Other info about how the payload have been received.</param>
    public void RaisePayloadParallel(ReadOnlySpan<EntityUid?> devices, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!_parallelHandlers.TryGetValue(payload.GetType(), out var handler))
            return;

        handler.RaisePayloadParallel(devices, ref payload, ref args);
    }
}

public abstract partial class DeviceNetworkHandler : EntitySystem
{
    [Dependency] protected SharedDeviceNetworkSystem DeviceSystem = default!;

    protected bool DeviceInitialized { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        InitializeDevice();
        LockSubscriptions();
        Register();
        DeviceInitialized = true;
    }

    /// <summary>
    /// A method to prepare subscriptions dictionary and other required objects.
    /// </summary>
    [MustCallBase]
    protected virtual void InitializeDevice() { }

    /// <summary>
    /// Locks the subscriptions before registering the handler.
    /// </summary>
    protected abstract void LockSubscriptions();

    /// <summary>
    /// Registers the handler in <see cref="SharedDeviceNetworkSystem"/>.
    /// </summary>
    protected abstract void Register();
}

/// <summary>
/// Handler that simply raises a <see cref="HandledNetworkPayload"/> on an entity.
/// </summary>
public interface IEntityDeviceNetworkHandler
{
    /// <summary>
    /// Raises a payload on a specified entity.
    /// </summary>
    /// <param name="uid">Uid of the target entity.</param>
    /// <param name="payload">Payload to send.</param>
    /// <param name="args">Other information about how the payload have been received.</param>
    void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args);
}

/// <summary>
/// Handler that supports running checks before sending a <see cref="HandledNetworkPayload"/> to the entity.
/// </summary>
public interface IBeforeDeviceNetworkHandler
{
    /// <summary>
    /// Runs checks before sending a network payload.
    /// </summary>
    /// <param name="uid">Uid of the target entity.</param>
    /// <param name="component">Component of the target entity.</param>
    /// <param name="args">Other information about how the payload have been received.</param>
    void RaiseBeforePayload(EntityUid uid, IComponent component, ref BeforePacketSentEvent args);
}

/// <summary>
/// Handler that supports sending <see cref="HandledNetworkPayload"/> to multiple entities in parallel.
/// </summary>
public interface IParallelDeviceNetworkHandler
{
    /// <summary>
    /// Raises a network payload on a span of devices in parallel.
    /// </summary>
    /// <param name="devices">Target entities to send the payload to.</param>
    /// <param name="payload">Payload to send.</param>
    /// <param name="args">Other information about how the payload have been received.</param>
    void RaisePayloadParallel(ReadOnlySpan<EntityUid?> devices, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args);
}

/// <summary>
/// A delegate for events that are defined in <see cref="DeviceNetworkHandler"/>s.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
/// <typeparam name="TN">Type of the network payload.</typeparam>
public delegate void DeviceNetworkPayloadHandler<TC, TN>(Entity<TC> ent, ref TN payload, ref DeviceNetworkPacketData args)
    where TC : IComponent
    where TN : HandledNetworkPayload;
