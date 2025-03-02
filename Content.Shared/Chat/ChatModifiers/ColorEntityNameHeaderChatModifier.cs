using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the [EntityNameHeader] tag in a [color] tag, should the player have colored names enabled.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ColorEntityNameHeaderChatModifier : ChatModifier
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private static ProtoId<ColorPalettePrototype> _chatNamePalette = "ChatNames";

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        IoCManager.InjectDependencies(this);

        var colorName = _config.GetCVar(CCVars.ChatEnableColorName);
        if (!colorName || !message.TryFirstOrDefault(x => x.Name == "EntityNameHeader", out var nameHeader))
            return message;

        var name = nameHeader.Value.StringValue;
        if (name == null)
            return message;

        var color = Color.TryFromHex(GetNameColor(name));
        if (color != null)
        {
            message.InsertOutsideTag(new MarkupNode("color", new MarkupParameter(color), null),
                "EntityNameHeader");
        }

        return message;
    }

    // CHAT-TODO: This whole thing needs to be remade such that it doesn't get the whole prototype list every time a message is sent.
    // Ask someone who knows better how to implement this. An IColorPaletteManager perhaps?
    public string GetNameColor(string name)
    {
        var nameColors = _prototypeManager.Index<ColorPalettePrototype>(_chatNamePalette).Colors.Values.ToArray();
        var chatNameColors = new string[nameColors.Length];
        for (var i = 0; i < nameColors.Length; i++)
        {
            chatNameColors[i] = nameColors[i].ToHex();
        }

        var colorIdx = Math.Abs(name.GetHashCode() % chatNameColors.Length);
        return chatNameColors[colorIdx];
    }
}
