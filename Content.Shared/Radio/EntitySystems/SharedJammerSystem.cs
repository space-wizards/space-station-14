using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Radio.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class JammerSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private DeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnItemToggle(Entity<RadioJammerComponent> entity, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            EnsureComp<ActiveRadioJammerComponent>(entity);
            EnsureComp<DeviceNetworkJammerComponent>(entity, out var jammingComp);
            _jammer.SetRange((entity, jammingComp), GetCurrentRange(entity));
            _jammer.AddJammableNetwork((entity, jammingComp), (int) DeviceNetIdDefaults.Wireless);

            // Add excluded frequencies using the system method
            foreach (var freq in entity.Comp.FrequenciesExcluded)
            {
                _jammer.AddExcludedFrequency((entity, jammingComp), freq);
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
        _popup.PopupEntity(message, args.User.Value, args.User.Value);
    }

    [SubscribeLocalEvent]
    private void OnRefreshChargeRate(Entity<RadioJammerComponent> entity, ref RefreshChargeRateEvent args)
    {
        if (_itemToggle.IsActivated(entity.Owner))
            args.NewChargeRate -= GetCurrentWattage(entity);
    }

    [SubscribeLocalEvent]
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

                    _popup.PopupEntity(Loc.GetString(setting.Message), user, user);
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancel(args.RadioSource, args.Channel.Frequency))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (ShouldCancel(args.RadioReceiver, args.Channel.Frequency))
            args.Cancelled = true;
    }

    private bool ShouldCancel(EntityUid sourceUid, DeviceFrequency frequency)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var jam, out var transform))
        {
            // Check if this jammer excludes the frequency
            if (jam.FrequenciesExcluded.Contains(frequency))
                continue;

            if (_transform.InRange(source, transform.Coordinates, GetCurrentRange((uid, jam))))
            {
                return true;
            }
        }

        return false;
    }

    private static float GetCurrentWattage(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Wattage;
    }

    private static float GetCurrentRange(Entity<RadioJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Range;
    }
}
