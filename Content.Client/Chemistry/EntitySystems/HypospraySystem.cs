using Content.Client.Chemistry.Components;
using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class HypospraySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HyposprayComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<HyposprayComponent, ItemStatusCollectMessage>(OnItemStatus);
    }

    private void OnHandleState(EntityUid uid, HyposprayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HyposprayComponentState cState)
            return;

        component.CurrentVolume = cState.CurVolume;
        component.TotalVolume = cState.MaxVolume;
        component.UiUpdateNeeded = true;
    }

    private void OnItemStatus(EntityUid uid, HyposprayComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new HyposprayStatusControl(component));
    }
}
