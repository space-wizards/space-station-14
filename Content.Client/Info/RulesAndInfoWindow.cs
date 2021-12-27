using Content.Client.EscapeMenu.UI;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Info
{
    public sealed class RulesAndInfoWindow : SS14Window
    {
        [Dependency] private readonly RulesManager _rulesManager = default!;
        [Dependency] private readonly IResourceCache _resourceManager = default!;

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
            AddSection(rulesList, Loc.GetString("ui-rules-header"), "Rules.txt", true);
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

        private void AddSection(Info info, string title, string path, bool markup = false)
        {
            info.InfoContainer.AddChild(new InfoSection(title,
                _resourceManager.ContentFileReadAllText($"/Server Info/{path}"), markup));
        }

        protected override void Opened()
        {
            base.Opened();

            _rulesManager.SaveLastReadTime();
        }
    }
}
