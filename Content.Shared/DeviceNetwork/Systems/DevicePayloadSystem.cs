using System.Collections.Frozen;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

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
            DeviceSystem.HandlersCache.Add(payload, this);
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

    public void RaisePayload(EntityUid uid, ref HandledNetworkPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!PayloadSubs.TryGetValue(payload.GetType(), out var handler))
            return;

        if (!Query.TryComp(uid, out var comp))
            return;

        var del = (DeviceNetworkPayloadHandlerWrapper<T>) handler;
        del.Invoke((uid, comp), ref payload, ref args);
    }
}
