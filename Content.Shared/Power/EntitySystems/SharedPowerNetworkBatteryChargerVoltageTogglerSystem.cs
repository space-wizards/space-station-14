using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerNetworkBatteryChargerVoltageTogglerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerNetworkBatteryChargerVoltageTogglerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PowerNetworkBatteryChargerVoltageTogglerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnExamined(Entity<PowerNetworkBatteryChargerVoltageTogglerComponent> entity, ref ExaminedEvent args)
    {
        var voltage = entity.Comp.Settings[entity.Comp.SelectedVoltageLevel].Voltage;
        var voltageStringSimple = voltage switch
        {
            Voltage.High => "HV",
            Voltage.Medium => "MV",
            Voltage.Apc => "LV",
            _ => "Unknown",
        };
        var voltageString = Loc.GetString("power-switchable-voltage", ("voltage", voltageStringSimple));
        args.PushMarkup(Loc.GetString("power-network-battery-charger-voltage-toggler-examine", ("voltage", voltageString)));
    }

    private void OnGetVerb(Entity<PowerNetworkBatteryChargerVoltageTogglerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act wont work with index.
            // Needs it to be saved in the loop.
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.VoltageLevel,
                Disabled = entity.Comp.SelectedVoltageLevel == currIndex,
                Text = Loc.GetString(setting.Name),
                Act = () =>
                {
                    entity.Comp.SelectedVoltageLevel = currIndex;
                    Dirty(entity);

                    ChangeVoltage(entity, setting);
                }
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    protected virtual void ChangeVoltage(Entity<PowerNetworkBatteryChargerVoltageTogglerComponent> entity, VoltageSetting setting) {}
}
