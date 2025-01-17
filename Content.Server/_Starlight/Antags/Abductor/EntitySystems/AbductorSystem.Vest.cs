using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Inventory.Events;
using Content.Shared.Stealth.Components;
using Content.Shared.Mobs.Components;
using System;
using Content.Shared.ActionBlocker;
using System.Linq;
using Content.Shared.Popups;
using Content.Shared.Interaction;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    
    public void InitializeVest()
    {
        SubscribeLocalEvent<AbductorVestComponent, AfterInteractEvent>(OnVestInteract);
        SubscribeLocalEvent<AbductorVestComponent, ItemSwitchedEvent>(OnItemSwitch);
        SubscribeLocalEvent<AbductorVestComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<AbductorVestComponent, GotEquippedEvent>(OnEquipped);
    }
   
    private void OnEquipped(Entity<AbductorVestComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Equipee != null && !HasComp<StealthComponent>(args.Equipee) && ent.Comp.CurrentState != "combat")
        {
            AddComp<StealthComponent>(args.Equipee);
            AddComp<StealthOnMoveComponent>(args.Equipee);
        }
    }
   
    private void OnUnequipped(Entity<AbductorVestComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Equipee != null && HasComp<StealthComponent>(args.Equipee))
        {
            RemComp<StealthComponent>(args.Equipee);
            RemComp<StealthOnMoveComponent>(args.Equipee);
        }
    }
    
    private void OnItemSwitch(EntityUid uid, AbductorVestComponent component, ref ItemSwitchedEvent args)
    {
        
        component.CurrentState = args.State;
        
        var user = Transform(uid).ParentUid;
        
        if (args.State == "combat")
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "combat", clothingComponent);
            
            if (HasComp<MobStateComponent>(user) && HasComp<StealthComponent>(user))
            {
                RemComp<StealthComponent>(user);
                RemComp<StealthOnMoveComponent>(user);
            }
        }
        else
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, null, clothingComponent);
            
            if (HasComp<MobStateComponent>(user) && !HasComp<StealthComponent>(user))
            {
                AddComp<StealthComponent>(user);
                AddComp<StealthOnMoveComponent>(user);
            }
        }
    }
    
    private void OnVestInteract(Entity<AbductorVestComponent> ent, ref AfterInteractEvent args)
    {
        if (!_actionBlockerSystem.CanInstrumentInteract(args.User, args.Used, args.Target)) return;
        if (!args.Target.HasValue) return;

        if (TryComp<AbductorConsoleComponent>(args.Target, out var console))
        {
            console.Armor = GetNetEntity(ent);
            _popup.PopupEntity(Loc.GetString("abductors-ui-vest-linked"), args.User);
            return;
        }
    }
}
