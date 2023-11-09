using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

/// <summary>
/// Data for a radio channel which be created dynamically or loaded from a <see cref="RadioChannelPrototype"/>
/// </summary>
[Virtual, DataDefinition]
[Serializable, NetSerializable]
public partial class RadioChannel
{
    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Single-character prefix to determine what channel a message should be sent to.
    /// </summary>
    [DataField("keycode")]
    public char KeyCode = '\0';

    [DataField]
    public int Frequency;

    [DataField]
    public Color Color = Color.Lime;

    /// <summary>
    /// If channel is long range it doesn't require telecommunication server
    /// and messages can be sent across different stations
    /// </summary>
    [DataField]
    public bool LongRange;
}

[Prototype("radioChannel")]
public sealed class RadioChannelPrototype : RadioChannel, IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
