using System.Collections.Frozen;
using System.Diagnostics;
using Content.Shared.Chat.ContentMarkupTags;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class ColorValueContentTag : IContentMarkupTag
{
    public string Name => "ColorValue";

    // TODO: These values should probably be retrieved via yaml, and be customizable in options! This solution works for now!
    private readonly IReadOnlyDictionary<string, Color> _colors = new Dictionary<string, Color>
    {
        ["Base"] = Color.White,
        ["Speech"] = Color.LightGray,
        ["Emote"] = Color.LightGray,
        ["Whisper"] = Color.DarkGray,
        ["LOOC"] = Color.MediumTurquoise,
        ["OOC"] = Color.LightSkyBlue,
        ["Dead"] = Color.MediumPurple,
        ["IngameAnnouncement"] = Color.Yellow,
        ["IngameAlert"] = Color.Red,
        ["Server"] = Color.Orange,
        ["AdminChat"] = Color.HotPink,
        ["AdminAlert"] = Color.Red,
        ["Radio.Common"] = Color.FromHex("#2cdb2c"),
        ["Radio.Security"] = Color.FromHex("#ff4242"),
        ["Radio.Supply"] = Color.FromHex("#b48b57"),
        ["Radio.Command"] = Color.FromHex("#fcdf03"),
        ["Radio.Service"] = Color.FromHex("#539c00"),
        ["Radio.Science"] = Color.FromHex("#cd7ccd"),
        ["Radio.Engineering"] = Color.FromHex("#ff733c"),
        ["Radio.Medical"] = Color.FromHex("#57b8f0"),
        ["Radio.Syndicate"] = Color.FromHex("#8f4a4b"),
        ["Radio.Freelance"] = Color.FromHex("#f6ce64"),
        ["Radio.Binary"] = Color.FromHex("#2ed2fd"),
        ["Radio.CentCom"] = Color.FromHex("#2681a5"),
        ["Radio.Handheld"] = Color.FromHex("#967101"),
    }.ToFrozenDictionary();

    public List<MarkupNode>? ProcessOpeningTag(MarkupNode node, int randomSeed)
    {
        return new List<MarkupNode>() { new MarkupNode("color", new MarkupParameter(GetColor(node)), null) };
    }

    public List<MarkupNode>? ProcessClosingTag(MarkupNode node, int randomSeed)
    {
        return new List<MarkupNode>() { new MarkupNode("color", null, null, true) };
    }

    private Color GetColor(MarkupNode node)
    {
        var markupColor = node.Value.StringValue;
        if (markupColor != null && _colors.TryGetValue(markupColor, out var color))
            return color;

        // CHAT-TODO: Log erroneous color attempt.
        Logger.Debug("This should not run!");
        return Color.White;
    }
}
