using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class ColorValueContentTagProcessor : ContentMarkupTagProcessorBase
{
    public const string SupportedNodeName = "ColorValue";

    public static ProtoId<ColorPalettePrototype> ChatChannelDefaultColorsPalette = "ChatChannelsDefaultColors";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly string _markupColor;

    /// <inheritdoc />
    public ColorValueContentTagProcessor(string markupColor)
    {
        _markupColor = markupColor;
        IoCManager.InjectDependencies(this);
    }

    public override string Name => SupportedNodeName;

    public override IReadOnlyList<MarkupNode> ProcessOpeningTag(MarkupNode node)
    {
        return new [] { new MarkupNode("color", new MarkupParameter(GetColor(node)), null) };
    }

    public override IReadOnlyList<MarkupNode> ProcessCloser(MarkupNode node)
    {
        return new [] { new MarkupNode("color", null, null, true) };
    }

    private Color GetColor(MarkupNode node)
    {
        // CHAT-TODO: These values should probably be overridable by in game options!
        var chatChannelColors = _prototypeManager.Index(ChatChannelDefaultColorsPalette);
        if (chatChannelColors.Colors.TryGetValue(_markupColor, out var color))
            return color;

        // CHAT-TODO: Log erroneous color attempt.
        Logger.Debug("This should not run!");
        return Color.White;
    }

    public static bool TryCreate(
        MarkupNode node,
        ChatMessageContext context,
        [NotNullWhen(true)] out ContentMarkupTagProcessorBase? processor
    )
    {
        if (!node.Value.TryGetString(out var markupColor) || string.IsNullOrWhiteSpace(markupColor))
        {
            processor = null;
            return false;
        }

        processor = new ColorValueContentTagProcessor(markupColor);
        return true;
    }
}
