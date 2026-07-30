using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork;

/// <summary>
///     A named device network frequency. Useful for ensuring entity prototypes can communicate with each other.
/// </summary>
[Prototype]
public sealed partial class DeviceFrequencyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    // TODO Somehow Allow per-station or some other type of named but randomized frequencies?
    [DataField(required: true)]
    public uint Frequency;

    /// <summary>
    ///     Optional name for this frequency, for displaying in game.
    /// </summary>
    [DataField]
    public LocId? Name;
}
