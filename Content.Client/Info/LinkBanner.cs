using Content.Client.Changelog;
using Content.Client.Guidebook.Controls;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;

namespace Content.Client.Info
{
    public sealed class LinkBanner : BoxContainer
    {
        private readonly IConfigurationManager _cfg;

        private ValueList<(CVarDef<string> cVar, Button button)> _infoLinks;

        private RulesAndInfoWindow? _rulesWindow;
        private GuidebookWindow? _guidebookWindow;
        private ChangelogWindow? _changelogWindow;

        public LinkBanner()
        {
            var buttons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            AddChild(buttons);

            var uriOpener = IoCManager.Resolve<IUriOpener>();
            _cfg = IoCManager.Resolve<IConfigurationManager>();

            var rulesButton = new Button() { Text = Loc.GetString("server-info-rules-button"), ToggleMode = true };

            rulesButton.OnPressed += args =>
            {
                if (_rulesWindow is { IsOpen: true })
                {
                    _rulesWindow.Close();

                    return;
                }

                _rulesWindow = new RulesAndInfoWindow();

                _rulesWindow.OnClose += () => rulesButton.Pressed = false;

                _rulesWindow.OpenCentered();
            };

            buttons.AddChild(rulesButton);

            AddInfoButton("server-info-discord-button", CCVars.InfoLinksDiscord);
            AddInfoButton("server-info-website-button", CCVars.InfoLinksWebsite);
            AddInfoButton("server-info-wiki-button", CCVars.InfoLinksWiki);
            AddInfoButton("server-info-forum-button", CCVars.InfoLinksForum);

            var guidebookController = UserInterfaceManager.GetUIController<GuidebookUIController>();
            var guidebookButton = new Button() { Text = Loc.GetString("server-info-guidebook-button"), ToggleMode = true };

            guidebookButton.OnPressed += _ =>
            {
                _guidebookWindow = guidebookController.ToggleGuidebook();

                if (_guidebookWindow is not { IsOpen: true })
                    return;

                _guidebookWindow.OnClose += () => guidebookButton.Pressed = false;
            };

            buttons.AddChild(guidebookButton);

            var changelogController = UserInterfaceManager.GetUIController<ChangelogUIController>();
            var changelogButton = new ChangelogButton() { ToggleMode = true };

            changelogButton.OnPressed += args =>
            {
                _changelogWindow = changelogController.ToggleWindow();

                _changelogWindow.OnClose += () => changelogButton.Pressed = false;
            };

            buttons.AddChild(changelogButton);

            void AddInfoButton(string loc, CVarDef<string> cVar)
            {
                var button = new Button { Text = Loc.GetString(loc) };
                button.OnPressed += _ => uriOpener.OpenUri(_cfg.GetCVar(cVar));
                buttons.AddChild(button);
                _infoLinks.Add((cVar, button));
            }
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            if (_rulesWindow is { IsOpen: true })
            {
                _rulesWindow.Close();
            }

            if (_guidebookWindow is { IsOpen: true })
            {
                _guidebookWindow.Close();
            }

            if (_changelogWindow is { IsOpen: true })
            {
                _changelogWindow.Close();
            }
        }

        protected override void EnteredTree()
        {
            // LinkBanner is constructed before the client even connects to the server due to UI refactor stuff.
            // We need to update these buttons when the UI is shown.

            base.EnteredTree();

            foreach (var (cVar, link) in _infoLinks)
            {
                link.Visible = _cfg.GetCVar(cVar) != "";
            }
        }
    }
}
