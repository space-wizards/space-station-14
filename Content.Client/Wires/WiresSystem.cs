using Content.Shared.Wires;
using Robust.Shared.GameStates;

namespace Content.Client.Wires;

public sealed class WiresSystem : SharedWiresSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WiresPanelComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, WiresPanelComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WiresPanelComponentState state)
            return;
        component.IsPanelOpen = state.IsPanelOpen;
        component.IsPanelVisible = state.IsPanelVisible;
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, WiresPanelComponent panel)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, WiresVisuals.MaintenancePanelState, panel.IsPanelOpen && panel.IsPanelVisible, appearance);
    }
}