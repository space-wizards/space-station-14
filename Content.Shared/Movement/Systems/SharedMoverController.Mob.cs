using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeMob()
    {
        SubscribeLocalEvent<MobMoverComponent, ComponentGetState>(OnMobGetState);
        SubscribeLocalEvent<MobMoverComponent, ComponentHandleState>(OnMobHandleState);
    }

    private void OnMobHandleState(EntityUid uid, MobMoverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobMoverComponentState state) return;
        component.GrabRangeVV = state.GrabRange;
        component.PushStrengthVV = state.PushStrength;
    }

    private void OnMobGetState(EntityUid uid, MobMoverComponent component, ref ComponentGetState args)
    {
        args.State = new MobMoverComponentState(component.GrabRange, component.PushStrength);
    }

    [Serializable, NetSerializable]
    private sealed class MobMoverComponentState : ComponentState
    {
        public float GrabRange;
        public float PushStrength;

        public MobMoverComponentState(float grabRange, float pushStrength)
        {
            GrabRange = grabRange;
            PushStrength = pushStrength;
        }
    }
}
