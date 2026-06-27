using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

public delegate void DeviceNetworkPayloadComponentWrapper(
    Entity<IComponent> ent,
    ref HandledNetworkPayload payload,
    ref DeviceNetworkPacketData args);

/// <summary>
/// System that handles <see cref="NetworkPayload"/>s for entities with various components.
/// Use this if you want the same payload to be handled by entities with different components, for whatever reason.
/// </summary>
/// <remarks>
/// Generally you should use <see cref="DevicePayloadSystem{T}"/>.
/// </remarks>
public abstract partial class DevicePayloadComponentSystem : DeviceNetworkHandler, IEntityDeviceNetworkHandler
{
    public FrozenDictionary<Type, FrozenDictionary<Type, Delegate>> PayloadSubs { get; protected set; } = default!;
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _payloadSubsCache = new();

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
        // Cast a snowgrave spell onto this nested dictionary
        var dict = new Dictionary<Type, FrozenDictionary<Type, Delegate>>();
        foreach (var (payload, compDict) in _payloadSubsCache)
        {
            dict.Add(payload, compDict.ToFrozenDictionary());
        }
        PayloadSubs = dict.ToFrozenDictionary();
    }

    [UsedImplicitly]
    protected void SubscribePayload<TC, TN>(DeviceNetworkPayloadHandler<TC, TN> handler)
        where TN : HandledNetworkPayload
        where TC : IComponent
    {
        if (DeviceInitialized)
        {
            Log.Error($"Tried to register a device network payload handler in type {typeof(TN).Name} after initialize!");
            return;
        }

        // It needs to be wrapped so when raising the Delegate it can be down-casted without issues.
        DeviceNetworkPayloadComponentWrapper wrapper = (ent, ref basePayload, ref args) =>
        {
            var specificPayload = (TN) basePayload;
            handler((ent.Owner, (TC) ent.Comp), ref specificPayload, ref args);
            basePayload = specificPayload;
        };

        _payloadSubsCache.TryAdd(typeof(TN), new Dictionary<Type, Delegate>());
        if (_payloadSubsCache[typeof(TN)].TryAdd(typeof(TC), wrapper))
            return;

        Log.Error($"Duplicate payload subscription for payload {typeof(TN).Name}, component {typeof(TC).Name}");
    }

    /// <summary>
    /// Raises a <see cref="HandledNetworkPayload"/> on an entity, and processes subscriptions for each component.
    /// </summary>
    /// <param name="uid">The target entity to raise the payload on.</param>
    /// <param name="payload">The payload to raise.</param>
    /// <param name="args">Other data about how the network packet was received.</param>
    public void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!PayloadSubs.TryGetValue(payload.GetType(), out var compDelegatePair))
            return;

        foreach (var (compType, wrappedDel) in compDelegatePair)
        {
            // This specific place is the bottleneck that makes this implementation worse than DevicePayloadSystem.
            // In theory if you could somehow resolve it, you could remove the DevicePayloadSystem entirely!
            if (!EntityManager.TryGetComponent(uid, compType, out var comp))
                return;

            var del = (DeviceNetworkPayloadComponentWrapper) wrappedDel;
            del.Invoke((uid, comp), ref payload, ref args);
        }
    }
}
