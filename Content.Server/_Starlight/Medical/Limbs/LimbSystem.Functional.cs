using System.Linq;
using System.Reflection;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Humanoid;
using Content.Shared._Starlight.Medical.Body;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Content.Shared.Starlight.Medical.Surgery;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class LimbSystem : SharedLimbSystem
{
    private static MethodInfo? s_raiseLocalEventRefMethod;
    static LimbSystem()
        => s_raiseLocalEventRefMethod = typeof(LimbSystem)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == nameof(RaiseLocalEvent)
                     && m.IsGenericMethodDefinition)
            .FirstOrDefault(m =>
            {
                var pars = m.GetParameters();
                if (pars.Length != 3)
                    return false;

                if (pars[0].ParameterType != typeof(EntityUid))
                    return false;

                if (!pars[1].ParameterType.IsByRef)
                    return false;

                if (pars[2].ParameterType != typeof(bool))
                    return false;

                return true;
            });
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
        foreach (var comp in EntityManager.GetComponents(limb))
        {
            if (comp is IImplantable)
            {
                {
                    var eventType = typeof(LimbAttachedEvent<>).MakeGenericType(comp.GetType());
                    var limbAttachedEvent = Activator.CreateInstance(eventType, [limb.Owner, comp]);

                    if (limbAttachedEvent != null)
                    {
                        var closedMethod = s_raiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                        closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
                    }
                }

                foreach (var face in comp.GetType().GetInterfaces().Where(x => x.IsAssignableTo(typeof(IImplantable))))
                {
                    var eventType = typeof(LimbAttachedEvent<>).MakeGenericType(face);
                    var limbAttachedEvent = Activator.CreateInstance(eventType, [limb.Owner, comp]);

                    if (limbAttachedEvent != null)
                    {
                        var closedMethod = s_raiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                        closedMethod.Invoke(this, [ body.Owner, limbAttachedEvent, false ]);
                    }
                }
            }
        }
    }

    private void RemoveLimb(Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
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
        foreach (var comp in EntityManager.GetComponents(limb))
        {
            if (comp is IImplantable)
            {
                {
                    var eventType = typeof(LimbRemovedEvent<>).MakeGenericType(comp.GetType());
                    var limbAttachedEvent = Activator.CreateInstance(eventType, limb.Owner, comp);
                    if (limbAttachedEvent != null)
                    {
                        var closedMethod = s_raiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                        closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
                    }
                }
                foreach (var face in comp.GetType().GetInterfaces().Where(x => x.IsAssignableTo(typeof(IImplantable))))
                {
                    var eventType = typeof(LimbRemovedEvent<>).MakeGenericType(face);
                    var limbAttachedEvent = Activator.CreateInstance(eventType, limb.Owner, comp);
                    if (limbAttachedEvent != null)
                    {
                        var closedMethod = s_raiseLocalEventRefMethod!.MakeGenericMethod(eventType);
                        closedMethod.Invoke(this, [body.Owner, limbAttachedEvent, false]);
                    }
                }
            }
        }
    }
}
