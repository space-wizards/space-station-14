using Robust.Client.GameObjects;

namespace Content.Client.Instruments.UI
{
    public sealed class InstrumentBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private InstrumentMenu? _instrumentMenu;

        [ViewVariables]
        public InstrumentComponent? Instrument { get; set; }

        public InstrumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            if (!EntMan.TryGetComponent<InstrumentComponent?>(Owner, out var instrument)) return;

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
