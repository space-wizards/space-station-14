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

            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var rulesButton = new Button() { Text = Loc.GetString("server-info-rules-button") };
            rulesButton.OnPressed += args => new RulesAndInfoWindow().Open();

            var discordButton = new Button {Text = Loc.GetString("server-info-discord-button") };
            discordButton.OnPressed += args => uriOpener.OpenUri(UILinks.Discord);

            var websiteButton = new Button {Text = Loc.GetString("server-info-website-button") };
            websiteButton.OnPressed += args => uriOpener.OpenUri(UILinks.Website);

            var wikiButton = new Button {Text = Loc.GetString("server-info-wiki-button") };
            wikiButton.OnPressed += args => uriOpener.OpenUri(UILinks.Wiki);

            var reportButton = new Button { Text = Loc.GetString("server-info-report-button") };
            reportButton.OnPressed += args => uriOpener.OpenUri(UILinks.BugReport);

            var creditsButton = new Button { Text = Loc.GetString("server-info-credits-button") };
            creditsButton.OnPressed += args => new CreditsWindow().Open();

            var changelogButton = new ChangelogButton
            {
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Right
            };

            buttons.AddChild(rulesButton);
            buttons.AddChild(discordButton);
            buttons.AddChild(websiteButton);
            buttons.AddChild(wikiButton);
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
