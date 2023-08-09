using Content.Shared.Examine;
using Robust.Shared.GameStates;

namespace Content.Shared.Wires;

public abstract class SharedWiresSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WiresPanelComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WiresPanelComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<WiresPanelComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnExamine(EntityUid uid, WiresPanelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(component.Open
            ? "wires-panel-component-on-examine-open"
            : "wires-panel-component-on-examine-closed"));
    }

    private void OnGetState(EntityUid uid, WiresPanelComponent component, ref ComponentGetState args)
    {
        args.State = new WiresPanelComponentState
        {
            Open = component.Open,
            Visible = component.Visible
        };
    }

    private void OnHandleState(EntityUid uid, WiresPanelComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WiresPanelComponentState state)
            return;
        component.Open = state.Open;
        component.Visible = state.Visible;
    }
}