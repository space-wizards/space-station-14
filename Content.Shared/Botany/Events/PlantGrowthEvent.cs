using Content.Shared.Botany.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Events;

/// <summary>
/// Event of plant growing ticking.
/// </summary>
[ByRefEvent]
[Serializable, NetSerializable]
public readonly record struct OnPlantGrowEvent(NetEntity Tray);

/// <summary>
/// Event raised when a harvest is attempted.
/// </summary>
[ByRefEvent]
public sealed class DoHarvestEvent(EntityUid user, EntityUid target) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}

/// <summary>
/// Event raised after a harvest is attempted.
/// </summary>
[ByRefEvent]
public sealed class AfterDoHarvestEvent(EntityUid user, EntityUid target) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}
