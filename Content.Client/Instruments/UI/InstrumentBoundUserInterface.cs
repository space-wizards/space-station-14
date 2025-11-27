using Content.Shared.ActionBlocker;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Content.Shared.Interaction;
using Robust.Client.Audio.Midi;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Instruments.UI
{
    public sealed class InstrumentBoundUserInterface : BoundUserInterface
    {
        public IEntityManager Entities => EntMan;
        [Dependency] public readonly IMidiManager MidiManager = default!;
        [Dependency] public readonly IFileDialogManager FileDialogManager = default!;
        [Dependency] public readonly ILocalizationManager Loc = default!;

        public readonly InstrumentSystem Instruments;
        public readonly ActionBlockerSystem ActionBlocker;
        public readonly SharedInteractionSystem Interactions;

        [ViewVariables] private InstrumentMenu? _instrumentMenu;

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
                _instrumentMenu?.PopulateBands(bandRx.Nearby, EntMan);
        }

        protected override void Open()
        {
            base.Open();

            _instrumentMenu = this.CreateWindow<InstrumentMenu>();
            _instrumentMenu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _instrumentMenu.RefreshBandsRequest += InstrumentMenu_RefreshBandsRequest;
            _instrumentMenu.SetBandMasterRequest += InstrumentMenu_SetBandMasterRequest;

            _instrumentMenu.SetMIDIAvailability(MidiManager.IsAvailable);

            if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            {
                _instrumentMenu.SetInstrument((Owner, instrument));
                instrument.OnMidiPlaybackEnded += Instrument_OnMidiPlaybackEnded;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (EntMan.TryGetComponent(Owner, out InstrumentComponent? instrument))
            {
                instrument.OnMidiPlaybackEnded -= Instrument_OnMidiPlaybackEnded;
            }
        }

        private void Instrument_OnMidiPlaybackEnded()
        {
            // Give the InstrumentSystem time to clear the renderer, preventing it from reusing the renderer it's about to dispose.
            Timer.Spawn(1000, () => { _instrumentMenu?.NotifyTrackEnded(); });
        }

        private void InstrumentMenu_SetBandMasterRequest(EntityUid ent)
        {
            Instruments.SetMaster(Owner, ent);
        }

        private void InstrumentMenu_RefreshBandsRequest()
        {
            SendMessage(new InstrumentBandRequestBuiMessage());
        }
    }
}
