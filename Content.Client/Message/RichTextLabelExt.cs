using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Message;

public static class RichTextLabelExt
{


     /// <summary>
     /// Sets the labels markup.
     /// </summary>
     /// <remarks>
     /// Invalid markup will cause exceptions to be thrown. Don't use this for user input!
     /// </remarks>
    public static RichTextLabel SetMarkup(this RichTextLabel label, string markup)
    {
        label.SetMessage(FormattedMessage.FromMarkup(markup));
        return label;
    }

     /// <summary>
     /// Sets the labels markup.<br/>
     /// Uses <c>FormatedMessage.FromMarkupPermissive</c> which treats invalid markup as text.
     /// </summary>
    public static RichTextLabel SetMarkupPermissive(this RichTextLabel label, string markup)
    {
        label.SetMessage(FormattedMessage.FromMarkupPermissive(markup));
        return label;
    }
}
