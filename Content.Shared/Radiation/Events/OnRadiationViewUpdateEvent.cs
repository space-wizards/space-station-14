using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Events;

/// <summary>
///     Raised on server as networked event when radiation system update its state
///     and emitted all rays from rad sources towards rad receivers.
///     Contains debug information about rad rays and all blockers on their way.
/// </summary>
/// <remarks>
///     Will be sent only to clients that activated radiation view using console command.
/// </remarks>
[Serializable, NetSerializable]
public sealed class OnRadiationViewUpdateEvent : EntityEventArgs
{
    /// <summary>
    ///     Total time in milliseconds that server took to do radiation processing.
    ///     Exclude time of entities reacting to <see cref="OnIrradiatedEvent"/>.
    /// </summary>
    public readonly double ElapsedTimeMs;

    /// <summary>
    ///     Total count of entities with <see cref="RadiationSourceComponent"/> on all maps.
    /// </summary>
    public readonly int SourcesCount;

    /// <summary>
    ///     Total count of entities with <see cref="RadiationReceiverComponent"/> on all maps.
    /// </summary>
    public readonly int ReceiversCount;

    /// <summary>
    ///     All radiation rays that was processed by radiation system.
    /// </summary>
    public readonly List<RadiationRay> Rays;

    public OnRadiationViewUpdateEvent(double elapsedTimeMs, int sourcesCount, int receiversCount, List<RadiationRay> rays)
    {
        ElapsedTimeMs = elapsedTimeMs;
        SourcesCount = sourcesCount;
        ReceiversCount = receiversCount;
        Rays = rays;
    }
}

/// <summary>
///     Raised when server enabled/disabled radiation debug view for client.
///     After that client will start/stop receiving <see cref="OnRadiationViewUpdateEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class OnRadiationViewToggledEvent : EntityEventArgs
{
    /// <summary>
    ///     Does debug radiation view enabled.
    /// </summary>
    public readonly bool IsEnabled;

    public OnRadiationViewToggledEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
