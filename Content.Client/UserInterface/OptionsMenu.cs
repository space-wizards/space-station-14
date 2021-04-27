using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
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

            GraphicsControl graphicsControl;
            KeyRebindControl rebindControl;
            AudioControl audioControl;

            var tabs = new TabContainer
            {
                Children =
                {
                    (graphicsControl = new GraphicsControl(_configManager, _prototypeManager)),
                    (rebindControl = new KeyRebindControl()),
                    (audioControl = new AudioControl(_configManager, _clydeAudio)),
                }
            };

            TabContainer.SetTabTitle(graphicsControl, Loc.GetString("ui-options-tab-graphics"));
            TabContainer.SetTabTitle(rebindControl, Loc.GetString("ui-options-tab-controls"));
            TabContainer.SetTabTitle(audioControl, Loc.GetString("ui-options-tab-audio"));

            Contents.AddChild(tabs);
        }
    }
}
