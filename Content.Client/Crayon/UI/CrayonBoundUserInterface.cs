using System.Linq;
using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Crayon
{
    public class CrayonBoundUserInterface : BoundUserInterface
    {
        public CrayonBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private CrayonWindow? _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new CrayonWindow(this);

            _menu.OnClose += Close;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var crayonDecals = prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
            if (crayonDecals != null)
                _menu.Populate(crayonDecals);
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _menu?.UpdateState((CrayonBoundUserInterfaceState) state);
        }

        public void Select(string state)
        {
            SendMessage(new CrayonSelectMessage(state));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _menu?.Close();
            }
        }
    }
}
