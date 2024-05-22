using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Message;

public static class RichTextLabelExt
{
    /**
     * Sets the labels markup.
     * <remarks>
     * Invalid markup will cause exceptions to be thrown. Don't use this for user input!
     * </remarks>
     */
    public static RichTextLabel SetMarkup(this RichTextLabel label, string markup)
    {
        label.SetMessage(FormattedMessage.FromMarkup(markup));
        return label;
    }

    /**
     * Sets the labels markup.<br/>
     * Uses <c>FormatedMessage.FromMarkupPermissive</c> which treats invalid markup as text.
     */
    public static RichTextLabel SetMarkupPermissive(this RichTextLabel label, string markup)
    {
        label.SetMessage(FormattedMessage.FromMarkupPermissive(markup));
        return label;
    }
}
