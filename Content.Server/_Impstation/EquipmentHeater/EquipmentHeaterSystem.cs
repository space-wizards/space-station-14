using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.EquipmentHeater;

public sealed class EquipmentHeaterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentHeaterComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<EquipmentHeaterComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    public static void OnGotEquipped(Entity<EquipmentHeaterComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.CurrentlyEquipped = args.Equipee;
    }

    public static void OnGotUnequipped(Entity<EquipmentHeaterComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.CurrentlyEquipped = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EquipmentHeaterComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.CurrentlyEquipped == null || !TryComp(comp.CurrentlyEquipped, out TemperatureComponent? temp))
                return;

            if (comp.NextUpdate < _timing.CurTime)
            {
                // increase the temperature of the equipee by the value set in the component. 
                _temperature.ForceChangeTemperature((EntityUid)comp.CurrentlyEquipped, temp.CurrentTemperature + comp.TempIncrease, temp);
                comp.NextUpdate = _timing.CurTime + comp.UpdateDelay;
            }
        }
    }
}
