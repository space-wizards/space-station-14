using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Instruments;

public sealed partial class SwappableInstrumentSystem : EntitySystem
{
    [Dependency] private SharedInstrumentSystem _sharedInstrument = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void AddStyleVerb(Entity<SwappableInstrumentComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || ent.Comp.InstrumentList.Count <= 1)
            return;

        SharedInstrumentComponent? instrument = null;
        if (!_sharedInstrument.ResolveInstrument(ent, ref instrument) || instrument is null)
            return;

        var user = args.User;
        var priority = 0;
        foreach (var entry in ent.Comp.InstrumentList)
        {
            var style = Loc.GetString(entry.Key);
            var selection = new AlternativeVerb
            {
                Text = style,
                Category = VerbCategory.InstrumentStyle,
                Priority = priority,
                Act = () =>
                {
                    _sharedInstrument.SetInstrumentProgram(ent, instrument, entry.Value.Item1, entry.Value.Item2);
                    _popup.PopupEntity(Loc.GetString("swappable-instrument-component-style-set", ("style", style)),
                        user, user);
                }
            };

            priority--;
            args.Verbs.Add(selection);
        }
    }
}
