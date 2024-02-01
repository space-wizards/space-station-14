using System.Linq;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Crayon.UI
{
    public sealed class CrayonBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CrayonWindow? _menu;

        public CrayonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = new CrayonWindow(this);

            _menu.OnClose += Close;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var crayonDecals = prototypeManager.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains("crayon"));
            _menu.Populate(crayonDecals);
            _menu.OpenCenteredLeft();
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

        public void SelectColor(Color color)
        {
            SendMessage(new CrayonColorMessage(color));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _menu?.Close();
                _menu = null;
            }
        }
    }
}
