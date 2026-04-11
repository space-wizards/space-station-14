using Content.Shared.Lathe;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Lathe.Components;

/// <summary>
/// This is used for a <see cref="LatheComponent"/> that releases heat into the surroundings while producing items.
/// </summary>
[RegisterComponent]
[Access(typeof(LatheSystem))]
public sealed partial class LatheHeatProducingComponent : Component
{
    /// <summary>
    /// The amount of heat produced from making an item, in Joules.
    /// Dumped into the tile the entity is on at <see cref="NextUpdate"/>.
    /// Can be negative.
    /// </summary>
    /// <remarks>Name is massively incorrect and I don't want to change it.
    /// This is just watts if you dump heat every second.</remarks>
    [DataField]
    public float EnergyPerSecond = 30000;

    /// <summary>
    /// How often the lathe should dump heat into the surroundings, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The next time the lathe should dump heat into the surroundings.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;
}
