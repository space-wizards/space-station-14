using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedVoltageTogglerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoltageTogglerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<VoltageTogglerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnExamined(Entity<VoltageTogglerComponent> entity, ref ExaminedEvent args)
    {
        var voltage = entity.Comp.Settings[entity.Comp.SelectedVoltageLevel].Voltage;
        args.PushMarkup(Loc.GetString(entity.Comp.ExamineText, ("voltage", VoltageString(voltage, true))));
    }

    private void OnGetVerb(Entity<VoltageTogglerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        var index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act won't work with index.
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
                    ChangeVoltage(entity, currIndex, user);
                }
            };

            var ev = new ToggleVoltageCheckEvent();
            RaiseLocalEvent(entity, ref ev);
            if (ev.DisableMessage != null)
            {
                verb.Message = ev.DisableMessage;
                verb.Disabled = true;
            }

            args.Verbs.Add(verb);
            index++;
        }
    }

    public string VoltageString(Voltage voltage, bool color=false)
    {
        var voltageNoColor = voltage switch
        {
            Voltage.High => "HV",
            Voltage.Medium => "MV",
            Voltage.Apc => "LV",
            _ => "Unknown",
        };

        if (color)
            return Loc.GetString("power-voltage", ("voltage", voltageNoColor));

        return voltageNoColor;
    }

    /// <summary>
    /// This is used by the GeneretorWindow.xaml.cs
    /// </summary>
    public Voltage GetVoltage(Entity<VoltageTogglerComponent> entity)
    {
        return entity.Comp.Settings[entity.Comp.SelectedVoltageLevel].Voltage;
    }

    /// <summary>
    /// This is used by the GeneretorWindow.xaml.cs
    /// </summary>
    public Voltage GetNextVoltage(Entity<VoltageTogglerComponent> entity)
    {
        var nextVoltageLevel = (entity.Comp.SelectedVoltageLevel + 1) % entity.Comp.Settings.Length;
        return entity.Comp.Settings[nextVoltageLevel].Voltage;
    }

    protected virtual void ChangeVoltage(Entity<VoltageTogglerComponent> entity, int settingIndex, EntityUid? user) {}
}

[ByRefEvent]
public record struct ToggleVoltageCheckEvent(string? DisableMessage = null);
