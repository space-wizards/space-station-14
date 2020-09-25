using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.GameObjects.Components.Crayon
{
    public class CrayonBoundUserInterface : BoundUserInterface
    {
        public CrayonBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private CrayonWindow _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new CrayonWindow(this);

            _menu.OnClose += Close;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var crayonDecals = prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
            if (crayonDecals != null)
                _menu.Populate(crayonDecals.Decals);
            _menu.OpenCentered();
        }

        public void Select(string state)
        {
            SendMessage(new CrayonSelectMessage(state));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _menu.Close();
        }
    }
}
