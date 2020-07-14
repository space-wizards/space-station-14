using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public class ServerInfo : VBoxContainer
    {
        private readonly RichTextLabel _richTextLabel;

        public ServerInfo()
        {
            _richTextLabel = new RichTextLabel
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            AddChild(_richTextLabel);

            var buttons = new HBoxContainer();
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var discordButton = new Button {Text = Loc.GetString("Join us on Discord!")};
            discordButton.OnPressed += args => uriOpener.OpenUri(UILinks.Discord);

            var websiteButton = new Button {Text = Loc.GetString("Website")};
            websiteButton.OnPressed += args => uriOpener.OpenUri(UILinks.Website);

            buttons.AddChild(discordButton);
            buttons.AddChild(websiteButton);
        }

        public void SetInfoBlob(string markup)
        {
            _richTextLabel.SetMessage(FormattedMessage.FromMarkup(markup));
        }
    }
}
