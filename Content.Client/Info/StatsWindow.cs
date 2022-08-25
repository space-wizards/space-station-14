using Content.Client.EscapeMenu.UI;
using Content.Shared.CCVar;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.GameTicking.Managers;
using System.Linq;
using Content.Client.CrewManifest;
using Content.Client.Eui;
using Content.Client.HUD.UI;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Info
{
    public sealed class StatsWindow : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        private OptionsMenu optionsMenu;
        public StatsWindow()
        {
            IoCManager.InjectDependencies(this);

            optionsMenu = new OptionsMenu();

            Title = Loc.GetString("ui-stats-title");

            var rootContainer = new TabContainer();

            var playtimes = new Info();

            rootContainer.AddChild(playtimes);

            TabContainer.SetTabTitle(playtimes, Loc.GetString("ui-stats-tab-playtimes"));

            PopulatePlaytimes(playtimes);

            Contents.AddChild(rootContainer);

            SetSize = (650, 650);
        }

        private void PopulatePlaytimes(Info playtimes)
        {
        }
    }
}
