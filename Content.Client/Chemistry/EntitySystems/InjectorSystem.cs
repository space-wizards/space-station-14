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
        Subs.ItemStatus<InjectorComponent>(ent => new InjectorStatusControl(ent));
        SubscribeLocalEvent<HyposprayComponent, ComponentHandleState>(OnHandleHyposprayState);
        Subs.ItemStatus<HyposprayComponent>(ent => new HyposprayStatusControl(ent));
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

    private void OnHandleHyposprayState(EntityUid uid, HyposprayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HyposprayComponentState cState)
            return;

        component.CurrentVolume = cState.CurVolume;
        component.TotalVolume = cState.MaxVolume;
        component.UiUpdateNeeded = true;
    }
}
