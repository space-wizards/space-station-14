namespace Content.Shared.Instruments;

public abstract class SharedInstrumentSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SharedInstrumentComponent, AfterAutoHandleStateEvent>(AfterHandleInstrumentState);
    }

    public virtual void SetupRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    {
    }

    public virtual void EndRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    {
    }

    public void SetInstrumentProgram(SharedInstrumentComponent component, byte program, byte bank)
    {
        component.InstrumentBank = bank;
        component.InstrumentProgram = program;
        Dirty(component);
    }

    private void AfterHandleInstrumentState(EntityUid uid, SharedInstrumentComponent instrument, ref AfterAutoHandleStateEvent args)
    {
        if(instrument.Playing)
            SetupRenderer(uid, true, instrument);
        else
            EndRenderer(uid, true, instrument);
    }
}
