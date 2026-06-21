using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract partial class SharedDeviceNetworkSystem
{
    private FrozenDictionary<Type, DeviceNetworkHandler> _handlers = default!;

    private readonly Dictionary<Type, DeviceNetworkHandler> _handlersCache = new();

    /// <summary>
    /// Method that registers this handler in <see cref="SharedDeviceNetworkSystem"/>.
    /// Must be called after all <see cref="DeviceNetworkHandler.PayloadSubs"/>> are filled in.
    /// </summary>
    public void RegisterHandler(DeviceNetworkHandler handler)
    {
        foreach (var payload in handler.PayloadSubs.Keys)
        {
            _handlersCache.Add(payload, handler);
        }
    }

    /// <summary>
    /// Must be called after all systems were initialized.
    ///  TODO ENGINE implement EntitySystem.PostInit()
    /// </summary>
    public void PostInit()
    {
        _handlers = _handlersCache.ToFrozenDictionary();
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
}

public abstract partial class DeviceNetworkHandler : EntitySystem
{
    [Dependency] private SharedDeviceNetworkSystem _device = default!;

    protected void Register()
    {
        _device.RegisterHandler(this);
    }

    /// <summary>
    /// Dictionary of payloads and the methods
    /// </summary>
    public abstract FrozenDictionary<Type, Delegate> PayloadSubs { get; protected set; }

    /// <summary>
    /// A method that fills the <see cref="PayloadSubs"/> dictionary with subscriptions.
    /// </summary>
    protected abstract void InitializeDevice();

    public abstract void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args);
}

/// <summary>
/// System that handles <see cref="NetworkPayload"/>s for entities that have component of type <see cref="T"/>.
/// </summary>
/// <typeparam name="T">Component that is being handled.</typeparam>
public abstract partial class DevicePayloadSystem<T> : DeviceNetworkHandler where T : IComponent
{
    [Dependency] private EntityQuery<T> _query = default!;

    public override FrozenDictionary<Type, Delegate> PayloadSubs { get; protected set; } = default!;
    private readonly Dictionary<Type, Delegate> _payloadSubsCache = new();

    private bool _isLocked;

    public override void Initialize()
    {
        base.Initialize();
        InitializeDevice();
        PayloadSubs = _payloadSubsCache.ToFrozenDictionary();
        Register();
        _isLocked = true;
    }

    protected void SubscribePayload<TN>(DeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
    {
        if (_isLocked)
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

        _payloadSubsCache.Add(typeof(TN), wrapper);
    }

    public override void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!PayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        if (!_query.TryComp(uid, out var comp))
            return;

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        del.Invoke((uid, comp), ref payload, ref args);
    }
}

public delegate void DeviceNetworkPayloadHandler<TC, TN>(Entity<TC> ent, ref TN payload, ref DeviceNetworkPacketData args)
    where TC : IComponent
    where TN : HandledNetworkPayload;

public delegate void DeviceNetworkPayloadHandlerWrapper<TC>(
    Entity<TC> ent,
    ref HandledNetworkPayload payload,
    ref DeviceNetworkPacketData args)
    where TC : IComponent;
