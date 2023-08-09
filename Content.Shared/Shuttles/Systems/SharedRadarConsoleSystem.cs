using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedRadarConsoleSystem : EntitySystem
{
    public const float DefaultMinRange = 64f;
    public const float DefaultMaxRange = 256f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<RadarConsoleComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, RadarConsoleComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RadarConsoleComponentState state)
            return;

        component.MaxRange = state.Range;
    }

    private void OnGetState(EntityUid uid, RadarConsoleComponent component, ref ComponentGetState args)
    {
        args.State = new RadarConsoleComponentState()
        {
            Range = component.MaxRange
        };
    }

    protected virtual void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
    }

    public void SetRange(EntityUid uid, float value, RadarConsoleComponent component)
    {
        if (component.MaxRange.Equals(value))
            return;

        component.MaxRange = value;
        Dirty(uid, component);
        UpdateState(uid, component);
    }

    [Serializable, NetSerializable]
    protected sealed class RadarConsoleComponentState : ComponentState
    {
        public float Range;
    }
}
