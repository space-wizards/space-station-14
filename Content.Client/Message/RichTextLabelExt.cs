using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Shared.Utility.Markup;

namespace Content.Client.Message
{
    public static class RichTextLabelExt
    {
        public static void SetMarkup(this RichTextLabel label, string markup)
        {
            label.SetMessage(Basic.RenderMarkup(markup));
        }
    }
}
