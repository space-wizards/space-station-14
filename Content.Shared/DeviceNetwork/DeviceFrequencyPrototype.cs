using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
///     A named device network frequency. Useful for ensuring entity prototypes can communicate with each other.
/// </summary>
[Prototype("deviceFrequency")]
[Serializable, NetSerializable]
public readonly record struct DeviceFrequencyPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    // TODO Somehow Allow per-station or some other type of named but randomized frequencies?
    [DataField("frequency", required: true)]
    public readonly uint Frequency;

    /// <summary>
    ///     Optional name for this frequency, for displaying in game.
    /// </summary>
    [DataField("name")] public readonly string? Name;

}
