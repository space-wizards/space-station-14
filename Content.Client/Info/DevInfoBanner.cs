using Content.Client.Changelog;
using Content.Client.Credits;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.Info
{
    public sealed class DevInfoBanner : BoxContainer
    {
        private CreditsWindow? _creditsWindow;

        public DevInfoBanner() {
            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();
            var cfg = IoCManager.Resolve<IConfigurationManager>();

            var bugReport = cfg.GetCVar(CCVars.InfoLinksBugReport);
            if (bugReport != "")
            {
                var reportButton = new Button {Text = Loc.GetString("server-info-report-button")};
                reportButton.OnPressed += args => uriOpener.OpenUri(bugReport);
                buttons.AddChild(reportButton);
            }

            var creditsButton = new Button { Text = Loc.GetString("server-info-credits-button"), ToggleMode = true };

            creditsButton.OnPressed += _ =>
            {
                if (_creditsWindow is { IsOpen: true })
                {
                    _creditsWindow.Close();

                    return;
                }

                _creditsWindow = new CreditsWindow();

                _creditsWindow.OnClose += () => creditsButton.Pressed = false;

                _creditsWindow.OpenCentered();
            };

            buttons.AddChild(creditsButton);
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            if (_creditsWindow is { IsOpen: true })
            {
                _creditsWindow.Close();
            }
        }
    }
}
