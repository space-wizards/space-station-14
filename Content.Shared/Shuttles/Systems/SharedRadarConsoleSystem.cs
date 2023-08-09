using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedRadarConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<RadarConsoleComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, RadarConsoleComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RadarConsoleComponentState state) return;
        component.MaxRange = state.Range;
    }

    private void OnGetState(EntityUid uid, RadarConsoleComponent component, ref ComponentGetState args)
    {
        args.State = new RadarConsoleComponentState()
        {
            Range = component.MaxRange
        };
    }

    protected virtual void UpdateState(RadarConsoleComponent component) {}

    public void SetRange(RadarConsoleComponent component, float value)
    {
        if (component.MaxRange.Equals(value)) return;
        component.MaxRange = value;
        Dirty(component);
        UpdateState(component);
    }

    [Serializable, NetSerializable]
    protected sealed class RadarConsoleComponentState : ComponentState
    {
        public float Range;
    }
}
