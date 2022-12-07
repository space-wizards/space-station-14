using Content.Server.Radiation.Systems;

namespace Content.Server.Radiation.Events;

/// <summary>
///     Raised when <see cref="RadiationSystem"/> updated all
///     radiation receivers and radiation sources.
/// </summary>
public record struct RadiationSystemUpdatedEvent;
