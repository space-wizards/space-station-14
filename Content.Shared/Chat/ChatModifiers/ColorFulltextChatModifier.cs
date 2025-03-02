using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the message in a [ColorValue="key"] tag.
/// This tag gets replaced with a [color] tag where the "key" attempts to match up to a clientside-selected color.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ColorFulltextChatModifier : ChatModifier
{
    [DataField]
    public string DefaultColorKey = "Base";

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {

        var colorKey = DefaultColorKey;
        if (chatMessageContext.TryGet<string>(ColorFulltextMarkupParameter.Color, out var color))
            colorKey = color;

        message.InsertAroundMessage(new MarkupNode("ColorValue", new MarkupParameter(colorKey), null, false));
        return message;
    }

    public enum ColorFulltextMarkupParameter
    {
        Color,
    }
}
