using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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

    /// <summary>
    /// Requirements that define who can write in this channel. Writing being non-managers sending to managers.
    /// </summary>
    /// <remarks>
    /// If null, this defaults to true.
    /// </remarks>
    [DataField]
    public BwoinkChannelRequirement? WriteRequirement { get; set; }
    /// <summary>
    /// Requirements that define who can read this channel. If this is condition is not met, a person will not be able to receive messages sent to them.
    /// </summary>
    /// <remarks>
    /// If null, this defaults to true.
    /// </remarks>
    [DataField]
    public BwoinkChannelRequirement? ReadRequirement { get; set; }
    /// <summary>
    /// Requirements that define who can manage this channel. Overrides write and read requirements.
    /// </summary>
    /// <remarks>
    /// If null, this defaults to true. Yes, really.
    /// </remarks>
    [DataField]
    public BwoinkChannelRequirement? ManageRequirement { get; set; }

    [DataField(required: true)]
    public List<BwoinkChannelFeature> Features { get; set; } = new();
}

public static class BwoinkChannelExtensions
{
    public static bool HasFeature<T>(this BwoinkChannelPrototype prototype) where T : BwoinkChannelFeature
    {
        return prototype.Features.Any(f => f is T);
    }

    public static bool TryGetFeature<T>(this BwoinkChannelPrototype prototype, [NotNullWhen(true)] out T? feature)
        where T : BwoinkChannelFeature
    {
        var hasValue = prototype.Features.TryFirstOrDefault(x => x is T, out var featureTemp);
        feature = hasValue ? featureTemp as T : null;
        return hasValue;
    }
}
