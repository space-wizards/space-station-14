using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class InjectorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InjectorComponent, ComponentHandleState>(OnHandleInjectorState);
        SubscribeLocalEvent<InjectorComponent, ItemStatusCollectMessage>(OnItemInjectorStatus);
        SubscribeLocalEvent<HyposprayComponent, ComponentHandleState>(OnHandleHyposprayState);
        SubscribeLocalEvent<HyposprayComponent, ItemStatusCollectMessage>(OnItemHyposprayStatus);
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
        args.Controls.Add(new InjectorStatusControl(component));
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
        args.Controls.Add(new HyposprayStatusControl(component));
    }
}
