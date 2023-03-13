using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedFloorOcclusionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<FloorOccluderComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<FloorOccluderComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(EntityUid uid, FloorOccluderComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherFixture.Body.Owner;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion) ||
            occlusion.Colliding.Contains(uid))
            return;

        SetEnabled(occlusion, true);
        occlusion.Colliding.Add(uid);
    }

    private void OnEndCollide(EntityUid uid, FloorOccluderComponent component, ref EndCollideEvent args)
    {
        var other = args.OtherFixture.Body.Owner;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion))
            return;

        occlusion.Colliding.Remove(uid);

        if (occlusion.Colliding.Count == 0)
            SetEnabled(occlusion, false);
    }

    protected virtual void SetEnabled(FloorOcclusionComponent component, bool enabled)
    {
        if (component.Enabled == enabled)
            return;

        component.Enabled = enabled;
        Dirty(component);
    }

    private void OnGetState(EntityUid uid, FloorOcclusionComponent component, ref ComponentGetState args)
    {
        args.State = new FloorOcclusionComponentState(component.Enabled, component.Colliding);
    }

    private void OnHandleState(EntityUid uid, FloorOcclusionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FloorOcclusionComponentState state)
            return;

        SetEnabled(component, state.Enabled);
        component.Colliding.Clear();
        component.Colliding.AddRange(state.Colliding);
    }

    [Serializable, NetSerializable]
    private sealed class FloorOcclusionComponentState : ComponentState
    {
        public readonly bool Enabled;
        public readonly List<EntityUid> Colliding;

        public FloorOcclusionComponentState(bool enabled, List<EntityUid> colliding)
        {
            Enabled = enabled;
            Colliding = colliding;
        }
    }
}
