using Content.Shared.Botany.Items.Components;
using Content.Shared.Burial.Components;

namespace Content.Shared.Botany.Events;

/// <summary>
/// Event raised when a botany hoe is used on a tray.
/// </summary>
[ByRefEvent]
public sealed class TrayHoeAttemptEvent(Entity<BotanyHoeComponent> hoe, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<BotanyHoeComponent> Hoe { get; } = hoe;
    public EntityUid User { get; } = user;
}

/// <summary>
/// Event raised when a produce is attempted to be composted.
/// </summary>
[ByRefEvent]
public sealed class CompostingProduceAttemptEvent(Entity<ProduceComponent> produce, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<ProduceComponent> Produce { get; } = produce;
    public EntityUid User { get; } = user;
}

/// <summary>
/// Event raised when a sample is attempted to be taken.
/// </summary>
[ByRefEvent]
public sealed class PlantSampleAttemptEvent(Entity<BotanySampleTakerComponent> sample, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<BotanySampleTakerComponent> Sample { get; } = sample;
    public EntityUid User { get; } = user;
}

/// <summary>
/// Event raised when a seed is attempted to be planted.
/// </summary>
[ByRefEvent]
public sealed class PlantingSeedAttemptEvent(Entity<SeedComponent> seed, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<SeedComponent> Seed { get; } = seed;
    public EntityUid User { get; } = user;
}

/// <summary>
/// Event raised when a shovel is attempted to be used.
/// </summary>
[ByRefEvent]
public sealed class TrayShovelAttemptEvent(Entity<ShovelComponent> shovel, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<ShovelComponent> Shovel { get; } = shovel;
    public EntityUid User { get; } = user;
}
