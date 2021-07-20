using Content.Client.EscapeMenu.UI.Tabs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.EscapeMenu.UI
{
    public sealed partial class OptionsMenu : SS14Window
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClydeAudio _clydeAudio = default!;

        public OptionsMenu()
        {
            SetSize = MinSize = (800, 450);
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("ui-options-title");

            GraphicsTab graphicsTab;
            KeyRebindTab rebindTab;
            AudioTab audioTab;

            var tabs = new TabContainer
            {
                Children =
                {
                    (graphicsTab = new GraphicsTab()),
                    (rebindTab = new KeyRebindTab()),
                    (audioTab = new AudioTab())
                }
            };

            TabContainer.SetTabTitle(graphicsTab, Loc.GetString("ui-options-tab-graphics"));
            TabContainer.SetTabTitle(rebindTab, Loc.GetString("ui-options-tab-controls"));
            TabContainer.SetTabTitle(audioTab, Loc.GetString("ui-options-tab-audio"));

            Contents.AddChild(tabs);
        }
    }
}
