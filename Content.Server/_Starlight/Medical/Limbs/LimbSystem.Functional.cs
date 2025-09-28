using Content.Server.Body.Systems;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class LimbSystem : SharedLimbSystem
{
    private void AddLimb(Entity<HumanoidAppearanceComponent> body, string slot, Entity<BodyPartComponent> limb)
    {
        switch (limb.Comp.PartType)
        {
            case BodyPartType.Arm:
                if (limb.Comp.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limb, limb.Comp.Symmetry == BodyPartSymmetry.Left ? "left hand" : "right hand", BodyPartType.Hand, out _);

                foreach (var slotId in limb.Comp.Children.Keys)
                {
                    if (slotId is null) continue;
                    var slotFullId = BodySystem.GetPartSlotContainerId(slotId);
                    var child = _containers.GetContainer(limb, slotFullId);

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Hand
                            && TryComp<HandsComponent>(body, out var hands))
                        {
                            _hands.AddHand((body, hands), slotFullId, limb.Comp.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                            AddLimbVisual(body, (containedEnt, innerPart));
                        }
                    }
                }
                break;
            case BodyPartType.Hand:
                if (TryComp<HandsComponent>(body, out var hands2))
                    _hands.AddHand((body, hands2), BodySystem.GetPartSlotContainerId(slot), limb.Comp.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                break;
            case BodyPartType.Leg:
                if (limb.Comp.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limb, limb.Comp.Symmetry == BodyPartSymmetry.Left ? "left foot" : "right foot", BodyPartType.Foot, out var slotId);

                foreach (var slotId in limb.Comp.Children.Keys)
                {
                    if (slotId is null) continue;
                    var slotFullId = BodySystem.GetPartSlotContainerId(slotId);
                    var child = _containers.GetContainer(limb, slotFullId);

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Foot)
                            AddLimbVisual(body, (containedEnt, innerPart));
                    }
                }
                break;
            case BodyPartType.Foot:
                break;
        }
        RaiseLimbAttachedEvent(body, limb);
    }

    private void RaiseLimbAttachedEvent(Entity<HumanoidAppearanceComponent> body, Entity<BodyPartComponent> limb)
    {
        var @event = new LimbAttachedEvent
        {
            Limb = limb,
            Body = body
        };
        RaiseLocalEvent(body, ref @event);
        RaiseLocalEvent(limb, ref @event);
    }

    private void RemoveLimb(Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        RaiseLimbPreDetachEvent(body, limb);

        switch (limb.Comp3.PartType)
        {
            case BodyPartType.Arm:
                foreach (var limbSlotId in limb.Comp3.Children.Keys)
                {
                    if (limbSlotId is null) continue;
                    var child = _containers.GetContainer(limb, BodySystem.GetPartSlotContainerId(limbSlotId));

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Hand
                            && TryComp<HandsComponent>(body, out var hands))
                            _hands.RemoveHand((body, hands), BodySystem.GetPartSlotContainerId(limbSlotId));
                    }
                }
                break;
            case BodyPartType.Hand:
                var parentSlot = _body.GetParentPartAndSlotOrNull(limb);
                if (parentSlot is not null && TryComp<HandsComponent>(body, out var hands2))
                    _hands.RemoveHand((body, hands2), BodySystem.GetPartSlotContainerId(parentSlot.Value.Slot));
                break;
            case BodyPartType.Leg:
            case BodyPartType.Foot:
                break;
        }

        RaiseLimbDetachedEvent(body, limb);
    }

    private void RaiseLimbDetachedEvent(
        Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body,
        Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        var @event = new LimbDetachedEvent
        {
            Limb = limb,
            Body = body
        };
        RaiseLocalEvent(body, ref @event);
        RaiseLocalEvent(limb, ref @event);
    }
    private void RaiseLimbPreDetachEvent(
        Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body,
        Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        var @event = new LimbPreDetachEvent
        {
            Limb = limb,
            Body = body
        };
        RaiseLocalEvent(body, ref @event);
        RaiseLocalEvent(limb, ref @event);
    }
}
