using Content.Client.Changelog;
using Content.Client.Credits;
using Content.Client.Links;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.Info
{
    public sealed class LinkBanner : BoxContainer
    {
        public LinkBanner()
        {
            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var rulesButton = new Button() {Text = Loc.GetString("server-info-rules-button")};
            rulesButton.OnPressed += args => new RulesAndInfoWindow().Open();

            var discordButton = new Button {Text = Loc.GetString("server-info-discord-button")};
            discordButton.OnPressed += args => uriOpener.OpenUri(UILinks.Discord);

            var websiteButton = new Button {Text = Loc.GetString("server-info-website-button")};
            websiteButton.OnPressed += args => uriOpener.OpenUri(UILinks.Website);

            var wikiButton = new Button {Text = Loc.GetString("server-info-wiki-button")};
            wikiButton.OnPressed += args => uriOpener.OpenUri(UILinks.Wiki);
            var changelogButton = new ChangelogButton();
            changelogButton.OnPressed += args => UserInterfaceManager.GetUIController<ChangelogUIController>().ToggleWindow();
            buttons.AddChild(changelogButton);
            buttons.AddChild(rulesButton);
            buttons.AddChild(discordButton);
            buttons.AddChild(websiteButton);
            buttons.AddChild(wikiButton);
        }
    }
}
