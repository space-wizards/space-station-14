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
        if (!DeviceSystem.BeforeHandlers.TryAdd(typeof(T), this))
            Log.Error($"Duplicate before payload subscription for component {typeof(T).Name}");
    }

    /// <summary>
    /// Raises the before event on a specific entity.
    /// </summary>
    public void RaiseBeforePayload(EntityUid uid, IComponent component, ref BeforePacketSentEvent args)
    {
        var ent = (Entity<T>) (uid, (T) component);
        OnBeforePayload(ent, ref args);
    }

    /// <summary>
    /// Implementation for the <see cref="RaiseBeforePayload"/> method.
    /// </summary>
    protected abstract void OnBeforePayload(Entity<T> ent, ref BeforePacketSentEvent args);
}
