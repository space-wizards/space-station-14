using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
        if (component.Open == false)
        {
            args.PushMarkup(Loc.GetString("wires-panel-component-on-examine-closed"));
        }

        else
        {
            args.PushMarkup(Loc.GetString("wires-panel-component-on-examine-open"));

            if (component?.WiresPanelSecurityExamination != null)
            {
                args.PushMarkup(Loc.GetString(component.WiresPanelSecurityExamination));
            }
        }
    }

    private void OnGetState(EntityUid uid, WiresPanelComponent component, ref ComponentGetState args)
    {
        args.State = new WiresPanelComponentState
        {
            Open = component.Open,
            Visible = component.Visible,
            WiresPanelSecurityExamination = component.WiresPanelSecurityExamination,
            WiresAccessible = component.WiresAccessible,
        };
    }

    private void OnHandleState(EntityUid uid, WiresPanelComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WiresPanelComponentState state)
            return;

        component.Open = state.Open;
        component.Visible = state.Visible;
        component.WiresPanelSecurityExamination = state.WiresPanelSecurityExamination;
        component.WiresAccessible = state.WiresAccessible;
    }
}
