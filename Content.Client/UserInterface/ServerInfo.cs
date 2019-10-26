using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public class ServerInfo : VBoxContainer
    {
        private const string DiscordUrl = "https://discordapp.com/invite/t2jac3p";
        private const string WebsiteUrl = "https://spacestation14.io";

        private readonly RichTextLabel _richTextLabel;

        public ServerInfo(ILocalizationManager localization)
        {
            _richTextLabel = new RichTextLabel
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            AddChild(_richTextLabel);

            var buttons = new HBoxContainer();
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var discordButton = new Button {Text = localization.GetString("Join us on Discord!")};
            discordButton.OnPressed += args => uriOpener.OpenUri(DiscordUrl);

            var websiteButton = new Button {Text = localization.GetString("Website")};
            websiteButton.OnPressed += args => uriOpener.OpenUri(WebsiteUrl);

            buttons.AddChild(discordButton);
            buttons.AddChild(websiteButton);
        }

        public void SetInfoBlob(string markup)
        {
            _richTextLabel.SetMessage(FormattedMessage.FromMarkup(markup));
        }
    }
}
