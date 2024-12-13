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
    public string ColorKey = "Base";

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {

        var colorKey = ColorKey;
        if (channelParameters.TryGetValue(ColorFulltextMarkupParameter.Color, out var color))
            colorKey = (string)color;

        return InsertAroundMessage(message, new MarkupNode("ColorValue", new MarkupParameter(colorKey), null, false));
    }

    public enum ColorFulltextMarkupParameter
    {
        Color,
    }
}
