using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// Handles before payload events in order to cancel their handling.
/// </summary>
public abstract partial class BeforeDevicePayloadSystem<T> : DevicePayloadSystem<T>, IBeforeDeviceNetworkHandler where T : IComponent
{
    protected override void Register()
    {
        base.Register();
        if (!DeviceSystem.BeforeHandlersCache.TryAdd(typeof(T), this))
            Log.Error($"Duplicate before payload subscription for component {typeof(T).Name}");
    }

    public void RaiseBeforePayload(EntityUid uid, IComponent component, ref BeforePacketSentEvent args)
    {
        var ent = (Entity<T>) (uid, (T) component);
        OnBeforePayload(ent, ref args);
    }

    protected abstract void OnBeforePayload(Entity<T> ent, ref BeforePacketSentEvent args);
}
