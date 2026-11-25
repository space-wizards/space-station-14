using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio;

/// <summary>
/// Defines a radio channel and its transmission properties.
/// </summary>
[Prototype]
public sealed partial class RadioChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Single-character prefix to determine what channel a message should be sent to.
    /// </summary>
    [DataField("keycode")]
    public char KeyCode { get; private set; } = '\0';

    /// <summary>
    /// Frequency used by the channel.
    /// </summary>
    [DataField]
    public FixedPoint2 Frequency { get; private set; } = 0;

    /// <summary>
    /// Color used to display the channel.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    /// <summary>
    /// Whether the channel can transmit across different stations without a telecommunications server.
    /// </summary>
    [DataField]
    public bool LongRange;
}
