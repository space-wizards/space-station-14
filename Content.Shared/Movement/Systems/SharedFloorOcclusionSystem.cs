using Content.Shared.Movement.Components;
using Content.Shared.StepTrigger.Components; // imp edit
using Content.Shared.StepTrigger.Systems; // imp edit
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
        SubscribeLocalEvent<FloorOccluderComponent, StepTriggeredOffEvent>(OnStepTriggered); // imp edit
        SubscribeLocalEvent<FloorOccluderComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt); // imp edit
    }

    private void OnStartCollide(Entity<FloorOccluderComponent> entity, ref StartCollideEvent args)
    {
        // imp edit - added StepTrigger check and moved logic to Occlude()
        if (HasComp<StepTriggerComponent>(entity))
            return;

        var other = args.OtherEntity;
        Occlude(entity, other);
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

    /// <summary>
    /// Imp: Occludes an entity. Moved from OnStartCollide() to allow it to be re-used in OnStepTriggered().
    /// </summary>
    private void Occlude(Entity<FloorOccluderComponent> ent, EntityUid other)
    {
        if (!TryComp<FloorOcclusionComponent>(other, out var occlusion) ||
            occlusion.Colliding.Contains(ent.Owner))
        {
            return;
        }

        occlusion.Colliding.Add(ent.Owner);
        Dirty(other, occlusion);
        SetEnabled((other, occlusion));
    }

    // imp edit
    private void OnStepTriggered(Entity<FloorOccluderComponent> entity, ref StepTriggeredOffEvent args)
    {
        var other = args.Tripper;
        Occlude(entity, other);
    }

    // imp edit
    private static void OnStepTriggerAttempt(Entity<FloorOccluderComponent> entity, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
}
