using Content.Server.Botany.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

/// <summary>
/// Data container for plant seed. Contains all info (values for components) for new plant to grow from seed.
/// </summary>
[RegisterComponent, Access(typeof(BotanySystem))]
public sealed partial class SeedComponent : SharedSeedComponent
{
    /// <summary>
    /// Seed data containing information about the plant type & properties that this seed can grow seed. If
    /// null, will instead attempt to get data from a seed prototype, if one is defined. See <see
    /// cref="SeedId"/>.
    /// </summary>
    [DataField]
    public SeedData? Seed;

    /// <summary>
    /// If not null, overrides the plant's initial health. Otherwise, the plant's initial health is set to the Endurance value.
    /// </summary>
    [DataField]
    public float? HealthOverride = null;

    /// <summary>
    /// Name of a base seed prototype that is used if <see cref="Seed"/> is null.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SeedPrototype>))]
    public string? SeedId;
}
