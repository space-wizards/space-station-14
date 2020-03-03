using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    public class GhostGui : Control
    {
        public GhostGui()
        {
            IoCManager.InjectDependencies(this);

            MouseFilter = MouseFilterMode.Ignore;

            AddChild(new Label(){Text = "YES THIS IS GHOST WHOOOOOO"});
        }
    }
}
