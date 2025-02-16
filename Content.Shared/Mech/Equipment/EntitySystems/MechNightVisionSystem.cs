using Content.Shared.Actions;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Mech;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Shared.Mech.Equipment.EntitySystems;

/// <summary>
/// 
/// </summary>
public sealed class MechNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechEquipmentActionComponent, MechToggleNightVisionEvent>(OnNightVisionToggle);
        
        SubscribeLocalEvent<MechComponent, BeforePilotInsertEvent>(OnPilotInsert);
        SubscribeLocalEvent<MechComponent, BeforePilotEjectEvent>(OnPilotEject);
    }

    private void OnNightVisionToggle(EntityUid uid, MechEquipmentActionComponent comp, ref MechToggleNightVisionEvent args)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComp) 
            || equipmentComp.EquipmentOwner == null 
            || !TryComp<MechComponent>(equipmentComp.EquipmentOwner, out var mechComp) 
            || mechComp.PilotSlot.ContainedEntity == null)
            return;
        
        if (!comp.EquipmentToggled && !HasComp<NightVisionComponent>(mechComp.PilotSlot.ContainedEntity.Value))
        {
            AddComp<NightVisionComponent>(mechComp.PilotSlot.ContainedEntity.Value);
            comp.EquipmentComponentAdded = true;
        }
        else
        {
            RemComp<NightVisionComponent>(mechComp.PilotSlot.ContainedEntity.Value);
            comp.EquipmentComponentAdded = false;
        }
        
        comp.EquipmentToggled = !comp.EquipmentToggled;
        
        _actions.SetToggled(comp.EquipmentActionEntity, comp.EquipmentToggled);
    }
    
    private void OnPilotInsert(EntityUid uid, MechComponent component, ref BeforePilotInsertEvent args)
    {
        if (HasComp<NightVisionComponent>(args.Pilot))
            return;
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
            if (HasComp<MechNightVisionComponent>(ent) && TryComp<MechEquipmentActionComponent>(ent, out var actionComp) && actionComp.EquipmentToggled)
            {
                AddComp<NightVisionComponent>(args.Pilot);
                actionComp.EquipmentComponentAdded = true;
            }
    }
    
    private void OnPilotEject(EntityUid uid, MechComponent component, ref BeforePilotEjectEvent args)
    {
        if (!HasComp<NightVisionComponent>(args.Pilot))
            return;
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
            if (HasComp<MechNightVisionComponent>(ent) && TryComp<MechEquipmentActionComponent>(ent, out var actionComp) && actionComp.EquipmentToggled && actionComp.EquipmentComponentAdded)
            {
                RemComp<NightVisionComponent>(args.Pilot);
                actionComp.EquipmentComponentAdded = false;
            }
    }
}
