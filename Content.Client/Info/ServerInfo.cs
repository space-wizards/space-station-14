using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Info
{
    public sealed class ServerInfo : BoxContainer
    {
        private readonly RichTextLabel _richTextLabel;

        public ServerInfo()
        {
            Orientation = LayoutOrientation.Vertical;

            _richTextLabel = new RichTextLabel
            {
                VerticalExpand = true
            };
            AddChild(_richTextLabel);
        }
        public void SetInfoBlob(string markup)
        {
            _richTextLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(markup));
        }
    }
}
