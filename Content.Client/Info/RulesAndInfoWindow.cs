using Content.Client.EscapeMenu.UI;
using Content.Shared.CCVar;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Client.Info
{
    public sealed class RulesAndInfoWindow : DefaultWindow
    {
        [Dependency] private readonly IResourceCache _resourceManager = default!;
        [Dependency] private readonly IConfigurationManager _cfgManager = default!;

        private OptionsMenu optionsMenu;

        public RulesAndInfoWindow()
        {
            IoCManager.InjectDependencies(this);

            optionsMenu = new OptionsMenu();

            Title = Loc.GetString("ui-info-title");

            var rootContainer = new TabContainer();

            var rulesList = new Info();
            var tutorialList = new Info();

            rootContainer.AddChild(rulesList);
            rootContainer.AddChild(tutorialList);

            TabContainer.SetTabTitle(rulesList, Loc.GetString("ui-info-tab-rules"));
            TabContainer.SetTabTitle(tutorialList, Loc.GetString("ui-info-tab-tutorial"));

            PopulateRules(rulesList);
            PopulateTutorial(tutorialList);

            Contents.AddChild(rootContainer);

            SetSize = (650, 650);
        }

        private void PopulateRules(Info rulesList)
        {
            AddSection(rulesList, MakeRules(_cfgManager, _resourceManager));
        }

        private void PopulateTutorial(Info tutorialList)
        {
            AddSection(tutorialList, Loc.GetString("ui-info-header-intro"), "Intro.txt");
            var infoControlSection = new InfoControlsSection();
            tutorialList.InfoContainer.AddChild(infoControlSection);
            AddSection(tutorialList, Loc.GetString("ui-info-header-gameplay"), "Gameplay.txt", true);
            AddSection(tutorialList, Loc.GetString("ui-info-header-sandbox"), "Sandbox.txt", true);

            infoControlSection.ControlsButton.OnPressed += _ => optionsMenu.OpenCentered();
        }

        private static void AddSection(Info info, Control control)
        {
            info.InfoContainer.AddChild(control);
        }

        private void AddSection(Info info, string title, string path, bool markup = false)
        {
            AddSection(info, MakeSection(title, path, markup, _resourceManager));
        }

        private static Control MakeSection(string title, string path, bool markup, IResourceManager res)
        {
            return new InfoSection(title, res.ContentFileReadAllText($"/Server Info/{path}"), markup);
        }

        public static Control MakeRules(IConfigurationManager cfg, IResourceManager res)
        {
            return MakeSection(Loc.GetString(cfg.GetCVar(CCVars.RulesHeader)), cfg.GetCVar(CCVars.RulesFile), true, res);
        }
    }
}
