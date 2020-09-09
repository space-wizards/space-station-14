using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu
    {
        private sealed class AudioControl : Control
        {
            public AudioControl(IConfigurationManager cfg)
            {
                AddChild(new Placeholder(IoCManager.Resolve<IResourceCache>())
                {
                    PlaceholderText = "Pretend there's a bunch of volume sliders here."
                });
            }
        }
    }
}
