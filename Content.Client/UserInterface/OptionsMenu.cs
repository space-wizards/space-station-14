using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

#nullable enable

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu : SS14Window
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IClydeAudio _clydeAudio = default!;

        public OptionsMenu()
        {
            SetSize = MinSize = (800, 450);
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Game Options");

            GraphicsControl graphicsControl;
            KeyRebindControl rebindControl;
            AudioControl audioControl;

            var tabs = new TabContainer
            {
                Children =
                {
                    (graphicsControl = new GraphicsControl(_configManager)),
                    (rebindControl = new KeyRebindControl()),
                    (audioControl = new AudioControl(_configManager, _clydeAudio)),
                }
            };

            TabContainer.SetTabTitle(graphicsControl, Loc.GetString("Graphics"));
            TabContainer.SetTabTitle(rebindControl, Loc.GetString("Controls"));
            TabContainer.SetTabTitle(audioControl, Loc.GetString("Audio"));

            Contents.AddChild(tabs);
        }
    }
}
