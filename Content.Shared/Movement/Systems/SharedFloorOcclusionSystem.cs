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

    private void OnStartCollide(Entity<FloorOccluderComponent> entity, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion) ||
            occlusion.Colliding.Contains(entity.Owner))
        {
            return;
        }

        occlusion.Colliding.Add(entity.Owner);
        Dirty(other, occlusion);
        SetEnabled((other, occlusion));
    }

    private void OnEndCollide(Entity<FloorOccluderComponent> entity, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion))
            return;

        if (!occlusion.Colliding.Remove(entity.Owner))
            return;

        Dirty(other, occlusion);
        SetEnabled((other, occlusion));
    }

    protected virtual void SetEnabled(Entity<FloorOcclusionComponent> entity)
    {

    }
}
