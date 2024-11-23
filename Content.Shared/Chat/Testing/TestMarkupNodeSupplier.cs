using Robust.Shared.Utility;

namespace Content.Shared.Chat.Testing;

[Serializable]
[DataDefinition]
public sealed partial class TestMarkupNodeSupplier : MarkupNodeSupplier
{
    public override FormattedMessage ProcessNodeSupplier(FormattedMessage message)
    {
        return InsertAroundString(message, new MarkupNode("TestMarkup", null, null, false), "testString");
    }
}
