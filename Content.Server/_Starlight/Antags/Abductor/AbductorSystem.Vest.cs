using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Starlight.Antags.Abductor;
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
    }
    
    private void OnItemSwitch(EntityUid uid, AbductorVestComponent component, ref ItemSwitchedEvent args)
    {
        if (args.State == "combat")
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "combat", clothingComponent);
        }
        else
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, null, clothingComponent);
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
