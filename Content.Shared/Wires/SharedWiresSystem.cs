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
    }

    private void OnExamine(EntityUid uid, WiresPanelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(component.IsPanelOpen
            ? "wires-panel-component-on-examine-open"
            : "wires-panel-component-on-examine-closed"));
    }

    private void OnGetState(EntityUid uid, WiresPanelComponent component, ref ComponentGetState args)
    {
        args.State = new WiresPanelComponentState
        {
            IsPanelOpen = component.IsPanelOpen,
            IsPanelVisible = component.IsPanelVisible
        };
    }
}