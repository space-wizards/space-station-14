using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

public abstract class SharedInstrumentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedInstrumentComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SharedInstrumentComponent, ComponentHandleState>(OnHandleState);
    }

    public virtual void SetupRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    { }

    public virtual void EndRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    { }

    private void OnGetState(EntityUid uid, SharedInstrumentComponent instrument, ref ComponentGetState args)
    {
        args.State =
            new InstrumentState(instrument.Playing, instrument.InstrumentProgram, instrument.InstrumentBank,
                instrument.AllowPercussion, instrument.AllowProgramChange, instrument.RespectMidiLimits, instrument.LastSequencerTick);
    }

    private void OnHandleState(EntityUid uid, SharedInstrumentComponent instrument, ref ComponentHandleState args)
    {
        if (args.Current is not InstrumentState state)
            return;

        if (state.Playing)
        {
            SetupRenderer(uid, true, instrument);
        }
        else
        {
            EndRenderer(uid, true, instrument);
        }

        instrument.Playing = state.Playing;
        instrument.AllowPercussion = state.AllowPercussion;
        instrument.AllowProgramChange = state.AllowProgramChange;
        instrument.InstrumentBank = state.InstrumentBank;
        instrument.InstrumentProgram = state.InstrumentProgram;
        instrument.DirtyRenderer = true;
    }
}
