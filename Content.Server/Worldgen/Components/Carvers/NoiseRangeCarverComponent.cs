using Content.Server._Citadel.Worldgen.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Citadel.Worldgen.Components.Carvers;

/// <summary>
///     This is used for carving out empty space in the game world, providing byways through the debris field.
/// </summary>
[RegisterComponent]
public sealed class NoiseRangeCarverComponent : Component
{
    /// <summary>
    ///     The noise channel to use as a density controller.
    /// </summary>
    /// <remarks>This noise channel should be mapped to exactly the range [0, 1] unless you want a lot of warnings in the log.</remarks>
    [DataField("noiseChannel", customTypeSerializer: typeof(PrototypeIdSerializer<NoiseChannelPrototype>))]
    public string NoiseChannel { get; } = default!;

    /// <summary>
    ///     The index of ranges in which to cut debris generation.
    /// </summary>
    [DataField("ranges", required: true)]
    public List<Vector2> Ranges { get; } = default!;
}

