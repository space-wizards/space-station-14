using Content.Client.Changelog;
using Content.Client.Credits;
using Content.Client.Links;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.Info
{
    public class ServerInfo : VBoxContainer
    {
        private readonly RichTextLabel _richTextLabel;

        public ServerInfo()
        {
            _richTextLabel = new RichTextLabel
            {
                VerticalExpand = true
            };
            AddChild(_richTextLabel);

            var buttons = new HBoxContainer();
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var discordButton = new Button {Text = Loc.GetString("Discord")};
            discordButton.OnPressed += args => uriOpener.OpenUri(UILinks.Discord);

            var websiteButton = new Button {Text = Loc.GetString("Website")};
            websiteButton.OnPressed += args => uriOpener.OpenUri(UILinks.Website);

            var reportButton = new Button { Text = Loc.GetString("Report Bugs") };
            reportButton.OnPressed += args => uriOpener.OpenUri(UILinks.BugReport);

            var creditsButton = new Button { Text = Loc.GetString("Credits") };
            creditsButton.OnPressed += args => new CreditsWindow().Open();

            var changelogButton = new ChangelogButton
            {
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Right
            };

            buttons.AddChild(discordButton);
            buttons.AddChild(websiteButton);
            buttons.AddChild(reportButton);
            buttons.AddChild(creditsButton);
            buttons.AddChild(changelogButton);
        }

        public void SetInfoBlob(string markup)
        {
            _richTextLabel.SetMessage(FormattedMessage.FromMarkup(markup));
        }
    }
}
