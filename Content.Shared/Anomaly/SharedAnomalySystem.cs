using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly;

public abstract class SharedAnomalySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySupercriticalComponent, ComponentGetState>(OnSupercriticalGetState);
        SubscribeLocalEvent<AnomalySupercriticalComponent, ComponentHandleState>(OnSupercriticalHandleState);
    }

    private void OnSupercriticalGetState(EntityUid uid, AnomalySupercriticalComponent component, ref ComponentGetState args)
    {
        args.State = new AnomalySupercriticalComponentState
        {
            EndTime = component.EndTime
        };
    }

    private void OnSupercriticalHandleState(EntityUid uid, AnomalySupercriticalComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AnomalySupercriticalComponentState state)
            return;

        component.EndTime = state.EndTime;
    }
}
