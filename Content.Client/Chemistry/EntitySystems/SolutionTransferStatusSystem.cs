using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class SolutionTransferStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InjectorComponent, ComponentHandleState>(OnHandleInjectorState);
        SubscribeLocalEvent<InjectorComponent, ItemStatusCollectMessage>(OnItemInjectorStatus);
        SubscribeLocalEvent<HyposprayComponent, ComponentHandleState>(OnHandleHyposprayState);
        SubscribeLocalEvent<HyposprayComponent, ItemStatusCollectMessage>(OnItemHyposprayStatus);
        SubscribeLocalEvent<SolutionTransferComponent, ComponentHandleState>(OnHandleTransferState);
        SubscribeLocalEvent<SolutionTransferComponent, ItemStatusCollectMessage>(OnItemTransferStatus);
    }

    private void OnHandleInjectorState(EntityUid uid, InjectorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedInjectorComponent.InjectorComponentState state)
        {
            return;
        }

        component.CurrentVolume = state.CurrentVolume;
        component.TotalVolume = state.TotalVolume;
        component.CurrentMode = state.CurrentMode;
        component.UiUpdateNeeded = true;
    }

    private void OnItemInjectorStatus(EntityUid uid, InjectorComponent component, ItemStatusCollectMessage args)
    {
        var tranlates = new TransferControlTranlates
        {
            DrawModeText = "injector-draw-text",
            InjectModeText = "injector-inject-text",
            InvalidModeText = "injector-invalid-injector-toggle-mode",
            VolumeLabelText = "injector-volume-label"
        };
        args.Controls.Add(new SolutionTransferStatusControl(component, tranlates, true, true));
    }

    private void OnHandleHyposprayState(EntityUid uid, HyposprayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HyposprayComponentState cState)
            return;

        component.CurrentVolume = cState.CurVolume;
        component.TotalVolume = cState.MaxVolume;
        component.UiUpdateNeeded = true;
    }

    private void OnItemHyposprayStatus(EntityUid uid, HyposprayComponent component, ItemStatusCollectMessage args)
    {

        var tranlates = new TransferControlTranlates
        {
            DrawModeText = "",
            InjectModeText = "",
            InvalidModeText = "",
            VolumeLabelText = "hypospray-volume-text"
        };
        args.Controls.Add(new SolutionTransferStatusControl(component, tranlates, true, false));
    }

    private void OnHandleTransferState(EntityUid uid, SolutionTransferComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedSolutionTransferComponent.SolutionTransferComponentState cState)
            return;

        component.CurrentVolume = cState.CurrentVolume;
        component.TotalVolume = cState.TotalVolume;
        component.CurrentMode = cState.CurrentMode;
        component.UiUpdateNeeded = true;
    }

    private void OnItemTransferStatus(EntityUid uid, SolutionTransferComponent component, ItemStatusCollectMessage args)
    {
        var tranlates = new TransferControlTranlates
        {
            DrawModeText = "comp-solution-transfer-draw-text",
            InjectModeText = "comp-solution-transfer-inject-text",
            InvalidModeText = "comp-solution-transfer-invalid-toggle-mode",
            VolumeLabelText = "comp-solution-transfer"
        };
        args.Controls.Add(new SolutionTransferStatusControl(component, tranlates, true, true));
    }
}
