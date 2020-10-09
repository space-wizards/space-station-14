using Content.Client.GameObjects.Components.Observer;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    public class GhostGui : Control
    {

        public readonly Button ReturnToBody = new Button() {Text = Loc.GetString("Return to body")};
        private GhostComponent _owner;

        public GhostGui(GhostComponent owner)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;

            MouseFilter = MouseFilterMode.Ignore;

            ReturnToBody.OnPressed += (args) => { owner.SendReturnToBodyMessage(); };

            AddChild(ReturnToBody);

            Update();
        }

        public void Update()
        {
            ReturnToBody.Disabled = !_owner.CanReturnToBody;
        }
    }
}
