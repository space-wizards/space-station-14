using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Humanoid;
using Content.Shared._Starlight.Medical.Body;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight.Medical.Surgery;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class LimbSystem : SharedLimbSystem
{
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly EntProtoId _virtual = "PartVirtual";
    public override void Initialize()
    {
        base.Initialize();
    }

    public bool AttachLimb(Entity<HumanoidAppearanceComponent> body, string slot, Entity<BodyPartComponent> part, Entity<BodyPartComponent> limb)
    {
        if (!_body.AttachPart(part, slot, limb, part.Comp, limb.Comp))
            return false;
        AddLimbVisual(body, limb);
        AddLimb(body, slot, limb);
        return true;
    }

    public bool AttachItem(EntityUid body, string slot, Entity<BodyPartComponent> part, Entity<MetaDataComponent> item)
    {
        var marker = EnsureComp<CustomLimbMarkerComponent>(item);

        var virtualItem = Spawn(_virtual);
        var virtualBodyPart = EnsureComp<BodyPartComponent>(virtualItem);
        var virtualCustomLimb = EnsureComp<CustomLimbComponent>(virtualItem);
        _metadata.SetEntityName(virtualItem, item.Comp.EntityName);

        marker.VirtualPart = virtualItem;
        virtualCustomLimb.Item = item;

        virtualBodyPart.PartType = BodyParts.GetBodyPart(slot);

        if (!_body.AttachPart(part, slot, virtualItem, part.Comp, virtualBodyPart))
        {
            QueueDel(virtualItem);
            return false;
        }
        AddItemLimb(body, slot, item);
        AddItemHand(body, item, BodySystem.GetPartSlotContainerId(slot));
        return true;
    }

    public void Amputatate(Entity<TransformComponent, HumanoidAppearanceComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb)
    {
        if (!_containers.TryGetContainingContainer((limb.Owner, limb.Comp1, limb.Comp2), out var container)
         || _body.GetParentPartAndSlotOrNull(limb.Owner) is not var (_, slotId)
         || !_containers.Remove(limb.Owner, container, destination: body.Comp1.Coordinates)) return;

        if (TryComp<CustomLimbComponent>(limb, out var virtualLimb))
            AmputateItemLimb((body, body.Comp1, body.Comp3), limb, slotId, virtualLimb);
        else
        {
            RemoveLimbVisual(body, limb);
            RemoveLimb(body, limb);
        }
    }

    private void AddItemLimb(EntityUid body, string slot, Entity<MetaDataComponent> item)
    {
        var layer = VisualLayers.GetLayer(slot);
        var vizualizer = EnsureComp<CustomLimbVisualizerComponent>(body);
        vizualizer.Layers[layer] = GetNetEntity(item);
        Dirty(body, vizualizer);
    }

    private void AmputateItemLimb(Entity<TransformComponent, BodyComponent> body, Entity<TransformComponent, MetaDataComponent, BodyPartComponent> limb, string slotId, CustomLimbComponent virtualLimb)
    {
        RemoveItemLimb(body, virtualLimb.Item, BodySystem.GetPartSlotContainerId(slotId));

        var vizualizer = EnsureComp<CustomLimbVisualizerComponent>(body);

        var layer = VisualLayers.GetLayer(slotId);
        vizualizer.Layers.Remove(layer);
        Dirty(body, vizualizer);
        QueueDel(limb.Owner);
    }

    private void AddItemHand(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!TryComp<HandsComponent>(bodyId, out var hands))
            return;

        if (!itemId.IsValid())
        {
            Log.Debug("no valid item");
            return;
        }

        _hands.AddHand((bodyId,hands), handId, HandLocation.Middle);
        _hands.DoPickup(bodyId, handId, itemId, hands);
        EnsureComp<UnremoveableComponent>(itemId);
    }

    private void RemoveItemLimb(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!bodyId.IsValid() || !itemId.IsValid()) return;

        RemComp<UnremoveableComponent>(itemId);
        _hands.DoDrop(itemId, handId);
        _hands.RemoveHand(bodyId, handId);
    }
}
