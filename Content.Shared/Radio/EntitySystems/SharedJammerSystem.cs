using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedJammerSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, ItemToggledEvent>(OnItemToggle);
        SubscribeLocalEvent<RadioJammerComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<RadioJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<RadioJammerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnItemToggle(Entity<RadioJammerComponent> entity, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            EnsureComp<ActiveRadioJammerComponent>(entity);
            EnsureComp<DeviceNetworkJammerComponent>(entity, out var jammingComp);
            _jammer.SetRange((entity, jammingComp), GetCurrentRange(entity));
            _jammer.AddJammableNetwork((entity, jammingComp), DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());

            // Add excluded frequencies using the system method
            foreach (var freq in entity.Comp.FrequenciesExcluded)
            {
                _jammer.AddExcludedFrequency((entity, jammingComp), (uint)freq);
            }
        }
        else
        {
            RemCompDeferred<ActiveRadioJammerComponent>(entity);
            RemCompDeferred<DeviceNetworkJammerComponent>(entity);
        }

        if (args.User == null)
            return;

        var state = Loc.GetString(args.Activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        _popup.PopupPredicted(message, args.User.Value, args.User.Value);
    }

    private void OnRefreshChargeRate(Entity<RadioJammerComponent> entity, ref RefreshChargeRateEvent args)
    {
        if (_itemToggle.IsActivated(entity.Owner))
            args.NewChargeRate -= GetCurrentWattage(entity);
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

                    // If the jammer is off, this won't do anything which is fine.
                    // The range should be updated when it turns on again!
                    _jammer.TrySetRange(entity.Owner, GetCurrentRange(entity));

                    _popup.PopupClient(Loc.GetString(setting.Message), user, user);
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    private void OnExamine(Entity<RadioJammerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var powerIndicator = _itemToggle.IsActivated(ent.Owner)
            ? Loc.GetString("radio-jammer-component-examine-on-state")
            : Loc.GetString("radio-jammer-component-examine-off-state");
        args.PushMarkup(powerIndicator);

        var powerLevel = Loc.GetString(ent.Comp.Settings[ent.Comp.SelectedPowerLevel].Name);
        var switchIndicator = Loc.GetString("radio-jammer-component-switch-setting", ("powerLevel", powerLevel));
        args.PushMarkup(switchIndicator);
    }

    private float GetCurrentWattage(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Wattage;
    }

    protected float GetCurrentRange(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Range;
    }
}
