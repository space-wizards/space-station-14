using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// A wrapper for the <see cref="DeviceNetworkPayloadHandler{TC, TN}"/> delegate.
/// </summary>
/// <typeparam name="TC">Type of the component.</typeparam>
public delegate void DeviceNetworkPayloadHandlerWrapper<TC>(
    Entity<TC> ent,
    ref HandledNetworkPayload payload,
    ref DeviceNetworkPacketData args)
    where TC : IComponent;

/// <summary>
/// System that handles <see cref="NetworkPayload"/>s for entities that have component of type <see cref="T"/>.
/// </summary>
/// <typeparam name="T">Component that is being handled.</typeparam>
public abstract partial class DevicePayloadSystem<T> : DeviceNetworkHandler, IEntityDeviceNetworkHandler where T : IComponent
{
    [Dependency] protected EntityQuery<T> Query = default!;

    public FrozenDictionary<Type, Delegate> PayloadSubs { get; protected set; } = default!;

    protected readonly Dictionary<Type, Delegate> PayloadSubsCache = new();

    protected override void Register()
    {
        foreach (var payload in PayloadSubs.Keys)
        {
            if (DeviceSystem.HandlersCache.TryAdd(payload, this))
                continue;

            Log.Error($"Duplicate payload subscription for payload {payload.Name}");
            return;
        }
    }

    protected override void LockSubscriptions()
    {
        PayloadSubs = PayloadSubsCache.ToFrozenDictionary();
    }

    [UsedImplicitly]
    protected void SubscribePayload<TN>(DeviceNetworkPayloadHandler<T, TN> handler) where TN : HandledNetworkPayload
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

        PayloadSubsCache.Add(typeof(TN), wrapper);
    }

    /// <summary>
    /// Raises a <see cref="HandledNetworkPayload"/> on an entity.
    /// </summary>
    /// <param name="uid">The target entity to raise the payload on.</param>
    /// <param name="payload">The payload to raise.</param>
    /// <param name="args">Other data about how the network packet was received.</param>
    public void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!Query.TryComp(uid, out var comp))
            return;

        if (!PayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        del.Invoke((uid, comp), ref payload, ref args);
    }
}
