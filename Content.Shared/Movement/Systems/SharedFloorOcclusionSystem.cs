using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Applies an occlusion shader for any relevant entities.
/// </summary>
public abstract class SharedFloorOcclusionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorOccluderComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<FloorOccluderComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(EntityUid uid, FloorOccluderComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion) ||
            occlusion.Colliding.Contains(uid))
        {
            return;
        }

        SetEnabled(other, occlusion, true);
        occlusion.Colliding.Add(uid);
    }

    private void OnEndCollide(EntityUid uid, FloorOccluderComponent component, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion))
            return;

        occlusion.Colliding.Remove(uid);

        if (occlusion.Colliding.Count == 0)
            SetEnabled(other, occlusion, false);
    }

    protected virtual void SetEnabled(EntityUid uid, FloorOcclusionComponent component, bool enabled)
    {
        if (component.Enabled == enabled)
            return;

        component.Enabled = enabled;
        Dirty(uid, component);
    }
}
