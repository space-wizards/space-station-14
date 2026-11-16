using Content.Shared.Placeable;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Shared.Cooking;

public sealed partial class CookingApplianceSystem : EntitySystem
{
    [Dependency] private CookingSystem _cooking = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiverSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CookingApplianceComponent, ItemPlacedEvent>(OnItemInserted);
        SubscribeLocalEvent<CookingApplianceComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<CookingApplianceComponent, PowerChangedEvent>(OnPowerChange);
        SubscribeLocalEvent<CookingApplianceComponent, HeatingSettingChangedEvent>(OnSettingChange);
    }

    /// <summary>
    /// Tries to start cooking the entity if it has the required cooking component
    /// </summary>
    private void OnItemInserted(Entity<CookingApplianceComponent> ent, ref ItemPlacedEvent args)
    {
        // Check if the entity has a cooking component, and if the appliance heater is turned on and powered
        if (!TryComp<CookingComponent>(args.OtherEntity, out var c)
            || !TryComp<EntityHeaterComponent>(ent, out var heater)
            || !_powerReceiverSystem.IsPowered(ent.Owner))
            return;
        
        if (heater.Setting == EntityHeaterSetting.Off)
            return;
        
        _cooking.TryStartCooking(args.OtherEntity, ent.Comp.ApplianceType);
    } 

    /// <summary>
    /// Stops the cooking process if the entity is removed from the appliance
    /// </summary>
    private void OnItemRemoved(Entity<CookingApplianceComponent> ent, ref ItemRemovedEvent args)
    {
        if (!TryComp<CookingComponent>(args.OtherEntity, out _))
            return;
        
        _cooking.StopCooking(args.OtherEntity);
    }
    
    private void OnPowerChange(Entity<CookingApplianceComponent> ent, ref PowerChangedEvent args)
    {
        CheckCookingStatus(ent, ent.Comp.ApplianceType); // Check cooking if the appliance power changes
    }

    private void OnSettingChange(Entity<CookingApplianceComponent> ent, ref HeatingSettingChangedEvent args)
    {
        CheckCookingStatus(ent, ent.Comp.ApplianceType); // Check cooking if the appliance is toggled
    }

    /// <summary>
    /// Checks if the entity should be cooking
    /// </summary>
    private void CheckCookingStatus(EntityUid ent, string type)
    {
        if (!TryComp<EntityHeaterComponent>(ent, out var heater) // this should also check for a cooling component for cooking frozen stuff, do later
            || !TryComp<ItemPlacerComponent>(ent, out var placer))
            return;

        if (heater.Setting == EntityHeaterSetting.Off || !_powerReceiverSystem.IsPowered(ent))
        {
            foreach (var item in placer.PlacedEntities)
            {
                Log.Debug("try stop cooking");
                _cooking.StopCooking(item);
            }
        }
        else
        {
            foreach (var item in placer.PlacedEntities)
            {
                Log.Debug("try start cooking");
                _cooking.TryStartCooking(item,type);
            }
        }
    }
}