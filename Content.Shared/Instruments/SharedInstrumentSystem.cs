using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared.Instruments;

public abstract class SharedInstrumentSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public abstract bool ResolveInstrument(EntityUid uid, ref SharedInstrumentComponent? component);

    public virtual void SetupRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    {
    }

    public virtual void EndRenderer(EntityUid uid, bool fromStateChange, SharedInstrumentComponent? instrument = null)
    {
    }

    public void SetInstrumentProgram(EntityUid uid, SharedInstrumentComponent component, byte program, byte bank)
    {
        component.Instrument.Bank = bank;
        component.Instrument.Program = program;
        Dirty(uid, component);
    }

    protected void AddStyleVerb(EntityUid entity,
        SharedInstrumentComponent comp,
        ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || comp.Instruments.Count <= 1)
            return;

        var priority = 0;
        // We have to pull the user out of the event as the event is a ref and we're using a lambda below.
        var user = args.User;

        foreach (var entry in comp.Instruments)
        {
            AlternativeVerb selection = new()
            {
                Text = entry.Key,
                Category = VerbCategory.InstrumentStyle,
                Priority = priority,
                Act = () =>
                {
                    SetInstrumentProgram(entity, comp, entry.Value.Program, entry.Value.Bank);

                    if (!_netMan.IsServer)
                        return;

                    var locString = Loc.GetString("swappable-instrument-component-style-set", ("style", entry.Key));
                    _popup.PopupEntity(locString, user, user);
                },
            };

            priority--;
            args.Verbs.Add(selection);
        }
    }
}
