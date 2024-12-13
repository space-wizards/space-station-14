using Robust.Shared.Utility;

namespace Content.Shared.Chat.Testing;

[Serializable]
[DataDefinition]
public sealed partial class TestChatModifier : ChatModifier
{
    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object>? channelParameters)
    {
        return InsertAroundString(message, new MarkupNode("TestMarkup", null, null, false), "testString");
    }
}
