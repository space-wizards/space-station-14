using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.Instruments.UI
{
    public sealed class InstrumentBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private InstrumentMenu? _instrumentMenu;

        public InstrumentComponent? Instrument { get; set; }

        public InstrumentBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<InstrumentComponent?>(Owner.Owner, out var instrument)) return;

            Instrument = instrument;
            _instrumentMenu = new InstrumentMenu(this);
            _instrumentMenu.OnClose += Close;

            _instrumentMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _instrumentMenu?.Dispose();
        }
    }
}
