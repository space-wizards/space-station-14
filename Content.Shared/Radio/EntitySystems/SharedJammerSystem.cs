using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedJammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<RadioJammerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnGetVerb(Entity<RadioJammerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        byte index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act wont work with index.
            // Needs it to be saved in the loop.
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.PowerLevel,
                Disabled = entity.Comp.SelectedPowerLevel == currIndex,
                Act = () =>
                {
                    entity.Comp.SelectedPowerLevel = currIndex;
                    Dirty(entity);
                    if (_jammer.TrySetRange(entity.Owner, GetCurrentRange(entity)))
                    {
                        Popup.PopupPredicted(Loc.GetString(setting.Message), user, user);
                    }
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    private void OnExamine(Entity<RadioJammerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var powerIndicator = HasComp<ActiveRadioJammerComponent>(ent)
                ? Loc.GetString("radio-jammer-component-examine-on-state")
                : Loc.GetString("radio-jammer-component-examine-off-state");
            args.PushMarkup(powerIndicator);

            var powerLevel = Loc.GetString(ent.Comp.Settings[ent.Comp.SelectedPowerLevel].Name);
            var switchIndicator = Loc.GetString("radio-jammer-component-switch-setting", ("powerLevel", powerLevel));
            args.PushMarkup(switchIndicator);
        }
    }

    public float GetCurrentWattage(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Wattage;
    }

    public float GetCurrentRange(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Range;
    }

    protected void ChangeLEDState(Entity<AppearanceComponent?> ent, bool isLEDOn)
    {
        _appearance.SetData(ent, RadioJammerVisuals.LEDOn, isLEDOn, ent.Comp);
    }

    protected void ChangeChargeLevel(Entity<AppearanceComponent?> ent, RadioJammerChargeLevel chargeLevel)
    {
        _appearance.SetData(ent, RadioJammerVisuals.ChargeLevel, chargeLevel, ent.Comp);
    }

}
