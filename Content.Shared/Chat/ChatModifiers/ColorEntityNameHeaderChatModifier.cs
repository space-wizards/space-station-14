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

        var color = GetNameColor(name);
        if (color != default)
        {
            message.InsertOutsideTag(new MarkupNode("color", new MarkupParameter(color), null),
                "EntityNameHeader");
        }

        return message;
    }

    public Color GetNameColor(string name)
    {
        var nameColors = _prototypeManager.Index<ColorPalettePrototype>(_chatNamePalette).Colors.Values;
        var colorIdx = Math.Abs(name.GetHashCode() % nameColors.Count);
        // simplified ElementAt, required to find element from ICollection without any allocations
        var i = 0;
        foreach (var nameColor in nameColors) 
        {
            if (i == colorIdx)
                return nameColor;
            i++;
        }

        return default;
    }
}
