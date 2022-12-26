using Content.Shared.Parallax.Biomes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

/// <summary>
/// Used for fake tiles on planets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class BiomeComponent : Component
{
    /// <summary>
    /// Seed to use for generating the random tiles.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("seed")]
    public int Seed;

    /// <summary>
    /// Biome prototype to use for this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<BiomePrototype>))]
    public string Prototype = string.Empty;
}
