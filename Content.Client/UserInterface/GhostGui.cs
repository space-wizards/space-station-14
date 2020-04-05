using System.Data;
using Content.Client.GameObjects.Components.Observer;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    public class GhostGui : Control
    {
        public Button ReturnToBody = new Button(){Text = "Return to body"};
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
