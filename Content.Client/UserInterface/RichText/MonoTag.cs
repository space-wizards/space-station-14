using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Sets the font to a monospaced variant
/// </summary>
public sealed class MonoTag : IMarkupTagHandler
{
    [Dependency] private readonly IFontSelectionManager _fontSelection = default!;

    public string Name => "mono";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var fontSize = FontTag.GetSizeForFontTag(context.Font, node);
        var font = _fontSelection.GetFont(StandardFontType.Monospace, fontSize);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
