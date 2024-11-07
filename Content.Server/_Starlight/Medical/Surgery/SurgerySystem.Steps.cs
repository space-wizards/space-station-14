using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Speech.Muting;
using NAudio.CoreAudioApi;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Starlight;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    public void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepBleedEffectComponent, SurgeryStepEvent>(OnStepBleedComplete);
        SubscribeLocalEvent<SurgeryClampBleedEffectComponent, SurgeryStepEvent>(OnStepClampBleedComplete);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepEmoteEffectComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);

        SubscribeLocalEvent<SurgeryStepOrganExtractComponent, SurgeryStepEvent>(OnStepOrganExtractComplete);
        SubscribeLocalEvent<SurgeryStepOrganInsertComponent, SurgeryStepEvent>(OnStepOrganInsertComplete);

        SubscribeLocalEvent<SurgeryStepAttachLimbEffectComponent, SurgeryStepEvent>(OnStepAttachLimbComplete);
        SubscribeLocalEvent<SurgeryStepAmputationEffectComponent, SurgeryStepEvent>(OnStepAmputationComplete);

        SubscribeLocalEvent<SurgeryRemoveAccentComponent, SurgeryStepEvent>(OnRemoveAccent);

    }

    private void OnStepBleedComplete(Entity<SurgeryStepBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if(ent.Comp.Damage is not null && TryComp<DamageableComponent>(args.Body, out var comp))
            _damageableSystem.SetDamage(args.Body, comp, ent.Comp.Damage);
        //todo add wound
    }

    private void OnStepClampBleedComplete(Entity<SurgeryClampBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
        //todo remove wound
    }
    private void OnStepOrganInsertComplete(Entity<SurgeryStepOrganInsertComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var organId)
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart)
            || !TryComp<OrganComponent>(organId, out var organComp))
            return;

        var part = args.Part;
        var body = args.Body;
        _delayAccumulator = 0;
        _delayQueue.Enqueue(() =>
        {
            if (_body.InsertOrgan(part, organId, ent.Comp.Slot, bodyPart, organComp)
            && TryComp<DamageableComponent>(organId, out var organDamageable)
            && TryComp<DamageableComponent>(body, out var bodyDamageable))
            {
                if (TryComp<OrganEyesComponent>(organId, out var organEyes)
                    && TryComp<BlindableComponent>(body, out var blindable))
                {
                    _blindable.SetMinDamage((body, blindable), organEyes.MinDamage ?? 0);
                    _blindable.AdjustEyeDamage((body, blindable), (organEyes.EyeDamage ?? 0) - blindable.MaxDamage);
                }
                if (TryComp<ImplantComponent>(organId, out var organImplant))
                {
                    if (organImplant.ImplantID == "Welding")
                        EnsureComp<EyeProtectionComponent>(body);
                }
                if (TryComp<OrganTongueComponent>(organId, out var organTongue)
                    && !organTongue.IsMuted)
                    RemComp<MutedComponent>(body);

                var change = _damageableSystem.TryChangeDamage(body, organDamageable.Damage, true, false, bodyDamageable);
                if (change is not null)
                    _damageableSystem.TryChangeDamage(organId, change.Invert(), true, false, organDamageable);
            }
        });
    }
    private void OnStepOrganExtractComplete(Entity<SurgeryStepOrganExtractComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Organ?.Count != 1) return;
        var organs = _body.GetPartOrgans(args.Part, Comp<BodyPartComponent>(args.Part));
        var type = ent.Comp.Organ.Values.First().Component.GetType();
        foreach (var organ in organs)
        {
            if (HasComp(organ.Id, type))
            {
                if (_body.RemoveOrgan(organ.Id, organ.Component)
                    && TryComp<OrganDamageComponent>(organ.Id, out var damageRule)
                    && damageRule.Damage is not null
                    && TryComp<DamageableComponent>(organ.Id, out var organDamageable)
                    && TryComp<DamageableComponent>(args.Body, out var bodyDamageable))
                {
                    if (TryComp<OrganEyesComponent>(organ.Id, out var organEyes)
                        && TryComp<BlindableComponent>(args.Body, out var blindable))
                    {
                        organEyes.EyeDamage = blindable.EyeDamage;
                        organEyes.MinDamage = blindable.MinDamage;
                        _blindable.UpdateIsBlind((args.Body, blindable));
                    }
                    if (TryComp<OrganTongueComponent>(organ.Id, out var organTongue))
                    {
                        organTongue.IsMuted = HasComp<MutedComponent>(args.Body);
                        AddComp<MutedComponent>(args.Body);
                    }
                    var change = _damageableSystem.TryChangeDamage(args.Body, damageRule.Damage.Invert(), true, false, bodyDamageable);
                    if (change is not null)
                        _damageableSystem.TryChangeDamage(organ.Id, change.Invert(), true, false, organDamageable);
                }
                return;
            }
        }
    }

    private void OnRemoveAccent(Entity<SurgeryRemoveAccentComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var accent in _accents)
            if (HasComp(args.Body, accent))
                RemCompDeferred(args.Body, accent);
    }

    private void OnStepEmoteEffectComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
        => _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Entity, xform.Coordinates);
    }
    private void OnStepAttachLimbComplete(Entity<SurgeryStepAttachLimbEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var limbId)
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart)
            || !TryComp<BodyPartComponent>(limbId, out var limb))
            return;

        var part = args.Part;
        var body = args.Body;

        _delayAccumulator = 0;
        _delayQueue.Enqueue(() =>
        {
            var slot = "";
            foreach (var slotTemp in _body.TryGetFreePartSlots(part, bodyPart))
            {
                slot = slotTemp;
                if (_body.AttachPart(part, slot, limbId, bodyPart, limb))
                    break;
            }

            if (TryComp<HumanoidAppearanceComponent>(body, out var humanoid)) //todo move to system
            {
                var limbs = _body.GetBodyPartAdjacentParts(limbId, limb).Except([part]).Concat([limbId]);
                foreach (var partLimbId in limbs)
                {
                    if (TryComp<BaseLayerIdComponent>(partLimbId, out var baseLayerStorage)
                        && TryComp(partLimbId, out BodyPartComponent? partLimb))
                    {
                        var layer = partLimb.ToHumanoidLayers();
                        if (layer is null) continue;
                        _humanoidAppearanceSystem.SetBaseLayerId(body, layer.Value, baseLayerStorage.Layer, true, humanoid);
                    }
                }
            }
            switch (limb.PartType)
            {
                case BodyPartType.Arm: //todo move to systems
                    if (limb.Children.Keys.Count == 0)
                        _body.TryCreatePartSlot(limbId, limb.Symmetry == BodyPartSymmetry.Left ? "left hand" : "right hand", BodyPartType.Hand, out var slotId);

                    foreach (var slotId in limb.Children.Keys)
                    {
                        if (slotId is null) continue;
                        var slotFullId = BodySystem.GetPartSlotContainerId(slotId);
                        var child = _containers.GetContainer(limbId, slotFullId);

                        foreach (var containedEnt in child.ContainedEntities)
                        {
                            if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                                && innerPart.PartType == BodyPartType.Hand)
                                _hands.AddHand(body, slotFullId, limb.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                        }
                    }
                    break;
                case BodyPartType.Hand:
                    _hands.AddHand(body, BodySystem.GetPartSlotContainerId(slot), limb.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                    break;
                case BodyPartType.Leg:
                    if (limb.Children.Keys.Count == 0)
                        _body.TryCreatePartSlot(limbId, limb.Symmetry == BodyPartSymmetry.Left ? "left foot" : "right foot", BodyPartType.Foot, out var slotId);
                    break;
                case BodyPartType.Foot:
                    break;
            }
        });
    }
    private void OnStepAmputationComplete(Entity<SurgeryStepAmputationEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform)
            && TryComp(args.Body, out BodyComponent? body)
            && TryComp(args.Part, out BodyPartComponent? limb))
        {

            if (!_containers.TryGetContainingContainer((args.Part, null, null), out var container)) return;
            if (_containers.Remove(args.Part, container, destination: xform.Coordinates))
            {
                if (TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoid)) //todo move to system
                {
                    var limbs = _body.GetBodyPartAdjacentParts(args.Part, limb).Concat([args.Part]); ;
                    foreach (var partLimbId in limbs)
                    {
                        if (TryComp<BaseLayerIdComponent>(partLimbId, out var baseLayerStorage)
                            && TryComp(partLimbId, out BodyPartComponent? partLimb))
                        {
                            var layer = partLimb.ToHumanoidLayers();
                            if (layer is null) continue;
                            if (humanoid.CustomBaseLayers.TryGetValue(layer.Value, out var customBaseLayer))
                                baseLayerStorage.Layer = customBaseLayer.Id;
                            else
                            {
                                var speciesProto = _prototypes.Index(humanoid.Species);
                                var baseSprites = _prototypes.Index<HumanoidSpeciesBaseSpritesPrototype>(speciesProto.SpriteSet);
                                if (baseSprites.Sprites.TryGetValue(layer.Value, out var baseLayer))
                                    baseLayerStorage.Layer = baseLayer;
                            }
                        }
                    }
                }
                switch (limb.PartType)
                {
                    case BodyPartType.Arm:  //todo move to systems
                        foreach (var slotId in limb.Children.Keys)
                        {
                            if (slotId is null) continue;
                            var child = _containers.GetContainer(args.Part, BodySystem.GetPartSlotContainerId(slotId));

                            foreach (var containedEnt in child.ContainedEntities)
                            {
                                if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                                    && innerPart.PartType == BodyPartType.Hand)
                                    _hands.RemoveHand(args.Body, BodySystem.GetPartSlotContainerId(slotId));
                            }
                        }
                        break;
                    case BodyPartType.Hand:
                        var parentSlot = _body.GetParentPartAndSlotOrNull(args.Part);
                        if (parentSlot is not null)
                            _hands.RemoveHand(args.Body, BodySystem.GetPartSlotContainerId(parentSlot.Value.Slot));
                        break;
                    case BodyPartType.Leg:
                    case BodyPartType.Foot:
                        break;
                }
            }
        }
    }
}
