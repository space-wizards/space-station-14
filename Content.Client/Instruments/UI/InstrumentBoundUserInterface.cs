using Content.Shared.ActionBlocker;
using Content.Shared.Instruments.UI;
using Content.Shared.Interaction;
using Robust.Client.Audio.Midi;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Instruments.UI
{
    public sealed class InstrumentBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;

        [ViewVariables] private InstrumentMenu? _instrumentMenu;
        public readonly InstrumentSystem Instruments;

        public InstrumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);

            Instruments = EntMan.System<InstrumentSystem>();
        }

        protected override void Open()
        {
            base.Open();

            _instrumentMenu = this.CreateWindow<InstrumentMenu>();
            _instrumentMenu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _instrumentMenu.RefreshBandsRequest += OnRefreshBandsRequest;
            _instrumentMenu.SetBandMasterRequest += OnSetBandMasterRequest;

            _instrumentMenu.SetMidiAvailability(_midiManager.IsAvailable);

            if (!EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
                return;

            _instrumentMenu.SetInstrument((Owner, instrument));
            instrument.OnMidiPlaybackEnded += OnMidiPlaybackEnded;
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is InstrumentBandResponseBuiMessage bandRx)
                _instrumentMenu?.PopulateBands(bandRx.Nearby, EntMan);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            {
                instrument.OnMidiPlaybackEnded -= OnMidiPlaybackEnded;
            }
        }

        private void OnMidiPlaybackEnded()
        {
            // Give the InstrumentSystem time to clear the renderer, preventing it from reusing the renderer it's about to dispose.
            Timer.Spawn(1000, () => { _instrumentMenu?.NotifyTrackEnded(); });
        }

        private void OnSetBandMasterRequest(EntityUid ent)
        {
            Instruments.SetMaster(Owner, ent);
        }

        private void OnRefreshBandsRequest()
        {
            SendMessage(new InstrumentBandRequestBuiMessage());
        }
    }
}
