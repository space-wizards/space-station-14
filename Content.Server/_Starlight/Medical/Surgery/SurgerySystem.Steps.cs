using System.Linq;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Traits.Assorted;
using Microsoft.CodeAnalysis;
using Content.Server._Starlight.Medical.Limbs;
using Content.Server.Administration.Systems;


namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
//  
//This file is already overloaded with responsibilities,
//it’s time to break its functionality into different systems.
//However, I don’t want to touch the official systems, so I need to come up with extensions for them.
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly LimbSystem _limbSystem = default!;
    [Dependency] private readonly StarlightEntitySystem _entity = default!;

    public void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepBleedEffectComponent, SurgeryStepEvent>(OnStepBleedComplete);
        SubscribeLocalEvent<SurgeryClampBleedEffectComponent, SurgeryStepEvent>(OnStepClampBleedComplete);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepEmoteEffectComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);

        SubscribeLocalEvent<SurgeryStepOrganExtractComponent, SurgeryStepEvent>(OnStepOrganExtractComplete);
        SubscribeLocalEvent<SurgeryStepOrganInsertComponent, SurgeryStepEvent>(OnStepOrganInsertComplete);

        SubscribeLocalEvent<SurgeryStepAttachLimbEffectComponent, SurgeryStepEvent>(OnStepAttachComplete);
        SubscribeLocalEvent<SurgeryStepAmputationEffectComponent, SurgeryStepEvent>(OnStepAmputationComplete);

        SubscribeLocalEvent<CustomLimbMarkerComponent, ComponentRemove>(CustomLimbRemoved);

        SubscribeLocalEvent<SurgeryRemoveAccentComponent, SurgeryStepEvent>(OnRemoveAccent);

    }

    private void OnStepAttachComplete(Entity<SurgeryStepAttachLimbEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (!_entity.TryGetSingleton(args.SurgeryProto, out var surgery)
            || !TryComp<SurgeryLimbSlotConditionComponent>(surgery, out var slotComp))
            return;

        OnStepAttachLimbComplete(ent, slotComp.Slot, ref args);
        if (slotComp.Slot != "head" && args.IsCancelled)
            OnStepAttachItemComplete(ent, slotComp.Slot, ref args);
    }

    private void OnStepBleedComplete(Entity<SurgeryStepBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Damage is not null && TryComp<DamageableComponent>(args.Body, out var comp))
            _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage);
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
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart))
            return;

        var containerId = SharedBodySystem.GetOrganContainerId(ent.Comp.Slot);

        if (ent.Comp.Slot == "cavity" && _containers.TryGetContainer(args.Part, containerId, out var container))
        {
            _containers.Insert(organId, container);
            return;
        }

        if (!TryComp<OrganComponent>(organId, out var organComp))
            return;

        var part = args.Part;
        var body = args.Body;

        if (!_body.InsertOrgan(part, organId, ent.Comp.Slot, bodyPart, organComp))
        {
            args.IsCancelled = true;
            return;
        }

        var ev = new SurgeryOrganImplantationCompleted(body, part, organId);
        RaiseLocalEvent(organId, ref ev);
    }
    private void OnStepOrganExtractComplete(Entity<SurgeryStepOrganExtractComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Organ?.Count != 1) return;

        var type = ent.Comp.Organ.Values.First().Component.GetType();

        if (ent.Comp.Slot != null && _containers.TryGetContainer(args.Part, SharedBodySystem.GetOrganContainerId(ent.Comp.Slot), out var container))
        {
            foreach (var containedEnt in container.ContainedEntities)
                if (HasComp(containedEnt, type))
                    _containers.Remove(containedEnt, container);

            return;
        }

        var organs = _body.GetPartOrgans(args.Part, Comp<BodyPartComponent>(args.Part));
        foreach (var organ in organs)
        {
            if (!HasComp(organ.Id, type) || !_body.RemoveOrgan(organ.Id, organ.Component)) continue;

            var ev = new SurgeryOrganExtracted(args.Body, args.Part, organ.Id);
            RaiseLocalEvent(organ.Id, ref ev);

            return;
        }
    }

    private void OnRemoveAccent(Entity<SurgeryRemoveAccentComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var accent in _accents)
            if (HasComp(args.Body, accent))
                RemCompDeferred(args.Body, accent);
    }

    private void OnStepEmoteEffectComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
    {
        
        if (!HasComp<PainNumbnessComponent>(args.Body))
        {
             _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
        }
    }

    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Entity, xform.Coordinates);
    }

    private void OnStepAttachLimbComplete(Entity<SurgeryStepAttachLimbEffectComponent> _, string slot, ref SurgeryStepEvent args) 
        => args.IsCancelled = args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var limdId)
            || !TryComp<BodyPartComponent>(limdId, out var limb)
            || !TryComp(args.Part, out BodyPartComponent? part)
            || !TryComp(args.Body, out HumanoidAppearanceComponent? humanoid)
            || !_limbSystem.AttachLimb((args.Body, humanoid), slot, (args.Part, part), (limdId, limb));

    private void OnStepAttachItemComplete(Entity<SurgeryStepAttachLimbEffectComponent> ent, string slot, ref SurgeryStepEvent args)
        => args.IsCancelled = args.Tools.Count == 0 
            || !(args.Tools.FirstOrDefault() is var itemId) 
            || !TryComp(itemId, out MetaDataComponent? metadata) 
            || HasComp<BodyPartComponent>(itemId) 
            || !TryComp(args.Part, out BodyPartComponent? limb) 
            || !_limbSystem.AttachItem(args.Body, slot, (args.Part, limb), (itemId, metadata));

    private void OnStepAmputationComplete(Entity<SurgeryStepAmputationEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (_entity.TryEntity<TransformComponent, HumanoidAppearanceComponent, BodyComponent>(args.Body, out var body) 
            && _entity.TryEntity<TransformComponent, MetaDataComponent, BodyPartComponent>(args.Part, out var limb))
            _limbSystem.Amputatate(body, limb);
    }

    private void CustomLimbRemoved(Entity<CustomLimbMarkerComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.VirtualPart is null) return;
        QueueDel(ent.Comp.VirtualPart.Value);
    }
}
