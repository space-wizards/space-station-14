using Content.Shared.ActionBlocker;
using Content.Shared.Instruments.UI;
using Content.Shared.Interaction;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Instruments.UI
{
    public sealed class InstrumentBoundUserInterface : BoundUserInterface
    {
        public IEntityManager Entities => EntMan;
        [Dependency] public readonly IMidiManager MidiManager = default!;
        [Dependency] public readonly IFileDialogManager FileDialogManager = default!;
        [Dependency] public readonly IPlayerManager PlayerManager = default!;
        [Dependency] public readonly ILocalizationManager Loc = default!;

        public readonly InstrumentSystem Instruments;
        public readonly ActionBlockerSystem ActionBlocker;
        public readonly SharedInteractionSystem Interactions;

        [ViewVariables] private InstrumentMenu? _instrumentMenu;
        [ViewVariables] private BandMenu? _bandMenu;
        [ViewVariables] private ChannelsMenu? _channelsMenu;

        [ViewVariables] public InstrumentComponent? Instrument { get; private set; }

        public InstrumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);

            Instruments = Entities.System<InstrumentSystem>();
            ActionBlocker = Entities.System<ActionBlockerSystem>();
            Interactions = Entities.System<SharedInteractionSystem>();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is InstrumentBandResponseBuiMessage bandRx)
                _bandMenu?.Populate(bandRx.Nearby, EntMan);
        }

        protected override void Open()
        {
            if (!EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
                return;

            Instrument = instrument;
            _instrumentMenu = new InstrumentMenu(this);
            _instrumentMenu.OnClose += Close;

            _instrumentMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _instrumentMenu?.Dispose();
            _bandMenu?.Dispose();
            _channelsMenu?.Dispose();
        }

        public void RefreshBands()
        {
            SendMessage(new InstrumentBandRequestBuiMessage());
        }

        public void OpenBandMenu()
        {
            _bandMenu ??= new BandMenu(this);

            // Refresh cache...
            RefreshBands();

            _bandMenu.OpenCenteredLeft();
        }

        public void CloseBandMenu()
        {
            if(_bandMenu?.IsOpen ?? false)
                _bandMenu.Close();
        }

        public void OpenChannelsMenu()
        {
            _channelsMenu ??= new ChannelsMenu(this);
            _channelsMenu.Populate();
            _channelsMenu.OpenCenteredRight();
        }

        public void CloseChannelsMenu()
        {
            if(_channelsMenu?.IsOpen ?? false)
                _channelsMenu.Close();
        }
    }
}
