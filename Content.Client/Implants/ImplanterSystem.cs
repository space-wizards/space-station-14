using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentHandleState>(OnHandleImplanterState);
        SubscribeLocalEvent<ImplanterComponent, ItemStatusCollectMessage>(OnItemImplanterStatus);
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ImplanterComponentState state)
            return;

        component.CurrentMode = state.CurrentMode;
        component.ImplantOnly = state.ImplantOnly;
        component.UiUpdateNeeded = true;
    }

    private void OnItemImplanterStatus(EntityUid uid, ImplanterComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new ImplanterStatusControl(component));
    }
}
