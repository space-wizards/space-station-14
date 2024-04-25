using Content.Shared.Popups;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Verbs;
using Content.Shared.RadioJammer;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedJammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
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
                    if (TryComp<DeviceNetworkJammerComponent>(entity.Owner, out var jammerComp))
                    {
                        // This is a little sketcy but only way to do it.
                        jammerComp.Range = GetCurrentRange(entity.Comp);
                        Dirty(entity.Owner, jammerComp);
                    }
                    Popup.PopupPredicted(Loc.GetString(setting.Message), user, user);
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    public float GetCurrentWattage(RadioJammerComponent jammer)
    {
        return jammer.Settings[jammer.SelectedPowerLevel].Wattage;
    }

    public float GetCurrentRange(RadioJammerComponent jammer)
    {
        return jammer.Settings[jammer.SelectedPowerLevel].Range;
    }

    protected void ChangeLEDState(bool isLEDOn, EntityUid uid,
        AppearanceComponent? appearance = null)
    {
        _appearance.SetData(uid, RadioJammerVisuals.LEDOn, isLEDOn, appearance);
    }

    protected void ChangeChargeLevel(RadioJammerChargeLevel chargeLevel, EntityUid uid,
        AppearanceComponent? appearance = null)
    {
        _appearance.SetData(uid, RadioJammerVisuals.ChargeLevel, chargeLevel, appearance);
    }

}
