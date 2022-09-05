using Content.Client.Atmos.Components;
using Content.Client.Atmos.UI;
using Content.Client.Items;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasAnalyzableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasAnalyzerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GasAnalyzerComponent, ItemStatusCollectMessage>(OnItemStatus);
    }

    private void OnHandleState(EntityUid uid, GasAnalyzerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedGasAnalyzerComponent.GasAnalyzerComponentState state)
            return;

        component.Danger = state.Danger;
        component.UiUpdateNeeded = true;
    }

    private void OnItemStatus(EntityUid uid, GasAnalyzerComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new GasAnalyzerStatusControl(component));
    }
}
