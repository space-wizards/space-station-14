using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Managers.Bwoink;

/// <summary>
/// This is used within the <see cref="SharedBwoinkManager"/> to determine what admin help channels are available.
/// </summary>
[Prototype]
public sealed class BwoinkChannelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name to display this channel as.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; set; } = default!;

    /// <summary>
    /// The text that's displayed when first opening the channel.
    /// </summary>
    [DataField(required: true)]
    public LocId HelpText { get; set; } = default!;

    /// <summary>
    /// The order of how this channel should be displayed on
    /// </summary>
    [DataField(required: true)]
    public int Order { get; set; } = 0;

    [DataField(required: true)]
    public List<BwoinkChannelFeature> Features { get; set; } = new();
}
