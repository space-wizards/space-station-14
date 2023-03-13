using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedFloorOcclusionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentHandleState>(OnHandleState);
    }

    protected virtual void SetEnabled(FloorOcclusionComponent component, bool enabled)
    {
        component.Enabled = enabled;
    }

    private void OnGetState(EntityUid uid, FloorOcclusionComponent component, ref ComponentGetState args)
    {
        args.State = new FloorOcclusionComponentState()
        {
            Enabled = component.Enabled
        };
    }

    private void OnHandleState(EntityUid uid, FloorOcclusionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FloorOcclusionComponentState state)
            return;

        SetEnabled(component, state.Enabled);
    }

    [Serializable, NetSerializable]
    private sealed class FloorOcclusionComponentState : ComponentState
    {
        public bool Enabled;
    }
}
