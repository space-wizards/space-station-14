using Content.Server.Instruments;
using Content.Shared.Instruments;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;

namespace Content.Server.GreyStation.Instruments;

public sealed class InstrumentWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<InstrumentWhitelistComponent> _query;
    private EntityQuery<ActivatableUIComponent> _uiQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<InstrumentWhitelistComponent>();
        _uiQuery = GetEntityQuery<ActivatableUIComponent>();

        SubscribeLocalEvent<InstrumentComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);

        SubscribeLocalEvent<AddInstrumentWhitelistComponent, MapInitEvent>(OnMapInit);
    }

    private void OnOpenAttempt(Entity<InstrumentComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        // only care about instruments not opening a pda etc
        if (_uiQuery.Comp(ent).Key is not InstrumentUiKey.Key)
            return;

        if (!_query.TryComp(args.User, out var comp))
            return;

        if (_whitelist.IsWhitelistFail(comp.Whitelist, ent))
        {
            _popup.PopupEntity(Loc.GetString(comp.FailPopup, ("instrument", ent)), args.User, args.User);
            args.Cancel();
        }
    }

    private void OnMapInit(Entity<AddInstrumentWhitelistComponent> ent, ref MapInitEvent args)
    {
        _query.CompOrNull(ent)?.Whitelist.Tags?.Add(ent.Comp.Tag);

        RemComp<AddInstrumentWhitelistComponent>(ent);
    }
}
