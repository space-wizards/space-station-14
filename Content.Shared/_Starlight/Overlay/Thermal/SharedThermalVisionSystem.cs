using System;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.Prototypes;
using static Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;

namespace Content.Shared.Eye.Blinding.Components;

public abstract class SharedThermalVisionSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    protected virtual bool IsPredict() => false;
    public EntProtoId Action = "ActionToggleThermal";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentShutdown>(OnVisionShutdown);
        SubscribeLocalEvent<ThermalVisionComponent, ToggleThermalVisionEvent>(OnToggleThermalVision);
    }
    private void OnVisionInit(Entity<ThermalVisionComponent> ent, ref ComponentInit args) 
        => _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, Action);

    private void OnVisionShutdown(Entity<ThermalVisionComponent> ent, ref ComponentShutdown args) 
        => _actionsSystem.RemoveAction(ent.Comp.ActionEntity);

    private void OnToggleThermalVision(Entity<ThermalVisionComponent> ent, ref ToggleThermalVisionEvent args)
    {
        if(args.Handled || IsPredict()) return;
        args.Handled = true;
        
        ent.Comp.Active = !ent.Comp.Active;

        if(ent.Comp.Active)
            ToggleOn(ent);
        else
            ToggleOff(ent);
    }
    protected virtual void ToggleOn(Entity<ThermalVisionComponent> ent)
    {
        
    }
    protected virtual void ToggleOff(Entity<ThermalVisionComponent> ent)
    {

    }
}

