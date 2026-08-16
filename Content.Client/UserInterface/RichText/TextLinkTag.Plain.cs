using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed partial class TextLinkTag
{
    private bool TryResolvePlainLink(MarkupNode node, out TextLinkData data)
    {
        data = default;

        if (!node.Attributes.TryGetValue(LinkAttributeName, out var linkParam) ||
            !linkParam.TryGetString(out var linkStr))
        {
            return false;
        }

        data = new TextLinkData(linkStr, null, true);
        return true;
    }
}
