using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// Handles <see cref="EntityHeaterComponent"/> events.
/// </summary>
public abstract partial class SharedEntityHeaterSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly int _settingCount = Enum.GetValues<EntityHeaterSetting>().Length;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<EntityHeaterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("entity-heater-examined", ("setting", ent.Comp.Setting)));
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<EntityHeaterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var nextSettingIndex = ((int)ent.Comp.Setting + 1) % _settingCount;
        var nextSetting = (EntityHeaterSetting)nextSettingIndex;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("entity-heater-switch-setting", ("setting", nextSetting)),
            Act = () =>
            {
                ChangeSetting(ent, nextSetting, user);
            }
        });
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<EntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        // disable heating element glowing layer if theres no power
        // doesn't actually change the setting since that would be annoying
        var setting = args.Powered ? ent.Comp.Setting : EntityHeaterSetting.Off;
        _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
        UpdateHeatBlurSetting(ent);
    }

    protected virtual void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        // Still allow changing the setting without power
        ent.Comp.Setting = setting;
        _audio.PlayPredicted(ent.Comp.SettingSound, ent, user);
        _popup.PopupEntity(Loc.GetString("entity-heater-switched-setting", ("setting", setting)), ent, user);
        Dirty(ent);

        // Only show the glowing heating element layer if there's power
        if (_receiver.IsPowered(ent.Owner))
            _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
        UpdateHeatBlurSetting(ent);
    }

    protected float SettingPower(EntityHeaterSetting setting, float max)
    {
        // Power use while off needs to be non-zero so powernet doesn't consider the device powered
        // by an unpowered network while in the off state. Otherwise, when we increase the load,
        // the clientside APC receiver will think the device is powered until it gets the next
        // update from the server, which will cause the heating element to glow for a moment.
        // I spent several hours trying to figure out a better way to do this using PowerDisabled
        // or something, but nothing worked as well as this.
        // Just think of the load as a little LED, or bad wiring, or something.
        return setting switch
        {
            EntityHeaterSetting.Low => max / 4f,
            EntityHeaterSetting.Medium => max / 2f,
            EntityHeaterSetting.High => max,
            _ => 0.01f,
        };
    }

    protected void UpdateHeatBlurSetting(Entity<EntityHeaterComponent> ent)
    {
        if (!TryComp<AnchoredBlurComponent>(ent, out var blur))
            return;

        var settingStep = blur.TargetStrength / 3;

        if (!_receiver.IsPowered(ent.Owner) || ent.Comp.Setting == EntityHeaterSetting.Off)
        {
            blur.Strength = 0f;
        }
        else
        {
            blur.Strength = ent.Comp.Setting switch
            {
                EntityHeaterSetting.Low => settingStep,
                EntityHeaterSetting.Medium => settingStep * 2,
                EntityHeaterSetting.High => settingStep * 3,
                _ => 0f,
            };
        }
        Dirty(ent, blur);
    }
}
