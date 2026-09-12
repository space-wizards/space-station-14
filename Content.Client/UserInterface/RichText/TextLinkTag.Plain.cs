using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed partial class TextLinkTag
{
    ///<summary>link="<string>" resolver. Always clickable, uses DefaultLinkColor for color.</summary>
    private bool TryResolvePlainLink(MarkupNode node, out LinkData data)
    {
        data = default;

        if (!node.Attributes.TryGetValue(LinkAttributeName, out var linkParam) ||
            !linkParam.TryGetString(out var linkStr))
        {
            return false;
        }

        data = new LinkData(LinkString: linkStr, LinkEntity: null, Color: null, Clickable: true);
        return true;
    }
}
