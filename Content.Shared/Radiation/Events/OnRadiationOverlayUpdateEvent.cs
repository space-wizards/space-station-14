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
public sealed class OnRadiationOverlayUpdateEvent(
    double elapsedTimeMs,
    int sourcesCount,
    int receiversCount,
    List<DebugRadiationRay> rays)
    : EntityEventArgs
{
    /// <summary>
    ///     Total time in milliseconds that server took to do radiation processing.
    ///     Exclude time of entities reacting to <see cref="OnIrradiatedEvent"/>.
    /// </summary>
    public readonly double ElapsedTimeMs = elapsedTimeMs;

    /// <summary>
    ///     Total count of entities with <see cref="RadiationSourceComponent"/> on all maps.
    /// </summary>
    public readonly int SourcesCount = sourcesCount;

    /// <summary>
    ///     Total count of entities with radiation receiver on all maps.
    /// </summary>
    public readonly int ReceiversCount = receiversCount;

    /// <summary>
    ///     All radiation rays that was processed by radiation system.
    /// </summary>
    public readonly List<DebugRadiationRay> Rays = rays;
}

/// <summary>
///     Raised when server enabled/disabled radiation debug view for client.
///     After that client will start/stop receiving <see cref="OnRadiationOverlayUpdateEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class OnRadiationOverlayToggledEvent : EntityEventArgs
{
    /// <summary>
    ///     Does debug radiation view enabled.
    /// </summary>
    public readonly bool IsEnabled;

    public OnRadiationOverlayToggledEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

/// <summary>
///     Raised when grid resistance was update for radiation overlay visualization.
/// </summary>
[Serializable, NetSerializable]
public sealed class OnRadiationOverlayResistanceUpdateEvent : EntityEventArgs
{
    /// <summary>
    ///     Key is grids uid. Values are tiles with their rad resistance.
    /// </summary>
    public readonly Dictionary<NetEntity, Dictionary<Vector2i, float>> Grids;

    public OnRadiationOverlayResistanceUpdateEvent(Dictionary<NetEntity, Dictionary<Vector2i, float>> grids)
    {
        Grids = grids;
    }
}
