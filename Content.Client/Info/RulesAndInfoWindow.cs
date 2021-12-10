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
            void AddSection(string title, string path, bool markup = false)
            {
                rulesList.InfoContainer.AddChild(new InfoSection(title,
                    _resourceManager.ContentFileReadAllText($"/Server Info/{path}"), markup));
            }

            AddSection(Loc.GetString("ui-info-header-rules"), "Rules.txt", true);
        }

        private void PopulateTutorial(Info tutorialList)
        {
            void AddSection(string title, string path, bool markup = false)
            {
                tutorialList.InfoContainer.AddChild(new InfoSection(title,
                    _resourceManager.ContentFileReadAllText($"/Server Info/{path}"), markup));
            }

            AddSection(Loc.GetString("ui-info-header-intro"), "Intro.txt");
            var infoControlSection = new InfoControlsSection();
            tutorialList.InfoContainer.AddChild(infoControlSection);
            AddSection(Loc.GetString("ui-info-header-gameplay"), "Gameplay.txt", true);
            AddSection(Loc.GetString("ui-info-header-sandbox"), "Sandbox.txt", true);

            infoControlSection.ControlsButton.OnPressed += _ => optionsMenu.OpenCentered();
        }

        protected override void Opened()
        {
            base.Opened();

            _rulesManager.SaveLastReadTime();
        }
    }
}
