using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Components;
using System.Linq;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class AlternativeBatteryAmmoModesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlternativeBatteryAmmoModesComponent, InteractHandEvent>(OnInteractHandEvent);
    }

    private void OnInteractHandEvent(EntityUid uid, AlternativeBatteryAmmoModesComponent component, InteractHandEvent args)
    {
        if (component.BatteryAmmoModes == null || !component.BatteryAmmoModes.Any())
            return;

        component.CurrentBatteryAmmoIndex++;
        if (component.CurrentBatteryAmmoIndex >= component.BatteryAmmoModes.Count)
        {
            component.CurrentBatteryAmmoIndex = 0;
        }

        var proto = component.BatteryAmmoModes[component.CurrentBatteryAmmoIndex].Prototype;
        var fireCost = component.BatteryAmmoModes[component.CurrentBatteryAmmoIndex].FireCost;

        if (proto != null && TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        {
            projectileBatteryAmmoProvider.Prototype = proto;
            projectileBatteryAmmoProvider.FireCost = fireCost;
        }
    }
}
