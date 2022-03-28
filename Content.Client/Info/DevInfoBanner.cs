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
    public sealed class DevInfoBanner : BoxContainer
    {
        public DevInfoBanner() {
            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();

            var reportButton = new Button {Text = Loc.GetString("server-info-report-button")};
            reportButton.OnPressed += args => uriOpener.OpenUri(UILinks.BugReport);

            var creditsButton = new Button {Text = Loc.GetString("server-info-credits-button")};
            creditsButton.OnPressed += args => new CreditsWindow().Open();
            buttons.AddChild(reportButton);
            buttons.AddChild(creditsButton);
        }
    }
}
