using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Starlight.Medical.Surgery;
using Content.Shared._Starlight.Medical.Damage;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.NukeOps;
using Content.Shared.Random.Helpers;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Robust.Server.Containers;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Medical;
public sealed class LimbDamageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BodyComponent, DamageBeforeApplyEvent>(OnDamage);
    }
    //duct tape solution
    private void OnDamage(Entity<BodyComponent> ent, ref DamageBeforeApplyEvent args)
    {
        //if (HasComp<NukeOperativeComponent>(ent)) return; // Nuke Ops are immune to limb damage. Temporary solution. mb Deathsquad?
        //if (!TryComp<HumanoidAppearanceComponent>(ent, out var appr) || appr.Species == "SlimePerson") return; // SlimePeople are immune to limb damage.

        //var chance = 0f;
        //foreach (var damage in args.Damage.DamageDict.Where(x => x.Value > 0))
        //    switch (damage.Key)
        //    {
        //        case "Blunt":
        //            chance += 0.00005f * damage.Value.Float();
        //            break;
        //        case "Slash":
        //            chance += 0.0005f * damage.Value.Float();
        //            break;
        //        case "Piercing":
        //            chance += 0.0001f * damage.Value.Float();
        //            break;
        //        case "Heat":
        //            chance += 0.0002f * damage.Value.Float();
        //            break;
        //        case "Cold":
        //            chance += 0.0004f * damage.Value.Float();
        //            break;
        //        case "Caustic":
        //            chance += 0.001f * damage.Value.Float();
        //            break;
        //        default:
        //            break;
        //    }
        //chance = Math.Clamp(chance, 0, 1);
        //if (_rand.Prob(chance))
        //{
        //    if (!TryRemoveLimb(ent, out var part)) return;
        //    Dirty(ent);
        //    QueueDel(part.Value.Owner);
        //    args.Cancelled = true;
        //}
    }

    //private bool TryRemoveLimb(Entity<BodyComponent> ent, [NotNullWhen(true)] out Entity<BodyPartComponent>? part)
    //{
    //    part = null;
    //    var root = _body.GetRootPartOrNull(ent.Owner);
    //    if (root is null) return false;
    //    var parts = _body.GetAllBodyPart(root.Value.Entity, root.Value.BodyPart)
    //        .Where(p => p.Comp.PartType != BodyPartType.Head)
    //        .ToList();
    //    if (parts.Count == 0) return false;
    //    part = _rand.Pick(parts);
    //    var parentPartAndSlot = _body.GetParentPartAndSlotOrNull(part.Value.Owner);
    //    if (parentPartAndSlot is null) return false;
    //    var (_, slotId) = parentPartAndSlot.Value;

    //    if (!_containers.TryGetContainingContainer((part.Value.Owner, null, null), out var container)) return false;
    //    if (!_containers.Remove(part.Value.Owner, container)) return false;
    //    if (TryComp<CustomLimbComponent>(part.Value.Owner, out var virtualLimb)
    //        && virtualLimb.Item.HasValue)
    //    {
    //        RemoveItemHand(ent.Owner, virtualLimb.Item.Value, BodySystem.GetPartSlotContainerId(slotId));

    //        var vizualizer = EnsureComp<CustomLimbVisualizerComponent>(ent.Owner);

    //        var layer = SurgerySystem.GetLayer(slotId);
    //        if (layer is not null)
    //        {
    //            vizualizer.Layers.Remove(layer.Value);
    //            Dirty(ent.Owner, vizualizer);
    //        }
    //    }
    //    else
    //    {
    //        switch (part.Value.Comp.PartType)
    //        {
    //            case BodyPartType.Arm:  //todo move to systems
    //                foreach (var limbSlotId in part.Value.Comp.Children.Keys)
    //                {
    //                    if (limbSlotId is null) continue;
    //                    var child = _containers.GetContainer(part.Value.Owner, BodySystem.GetPartSlotContainerId(limbSlotId));

    //                    foreach (var containedEnt in child.ContainedEntities)
    //                    {
    //                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
    //                            && innerPart.PartType == BodyPartType.Hand)
    //                            _hands.RemoveHand(ent.Owner, BodySystem.GetPartSlotContainerId(limbSlotId));
    //                    }
    //                }
    //                break;
    //            case BodyPartType.Hand:
    //                var parentSlot = _body.GetParentPartAndSlotOrNull(part.Value.Owner);
    //                if (parentSlot is not null)
    //                    _hands.RemoveHand(ent.Owner, BodySystem.GetPartSlotContainerId(parentSlot.Value.Slot));
    //                break;
    //            case BodyPartType.Leg:
    //            case BodyPartType.Foot:
    //                break;
    //        }
    //    }
    //    return true;
    //}

    //private void RemoveItemHand(EntityUid bodyId, EntityUid itemId, string handId)
    //{
    //    if (!TryComp<HandsComponent>(bodyId, out var hands)
    //        || !_hands.TryGetHand(bodyId, handId, out var hand, hands))
    //        return;

    //    if (!itemId.IsValid())
    //    {
    //        Log.Debug("no valid item");
    //        return;
    //    }
    //    RemComp<UnremoveableComponent>(itemId);
    //    _hands.DoDrop(itemId, hand);
    //    _hands.RemoveHand(bodyId, handId, hands);
    //}
}
