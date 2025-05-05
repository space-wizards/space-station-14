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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly int _settingCount = Enum.GetValues<EntityHeaterSetting>().Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntityHeaterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EntityHeaterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnExamined(Entity<EntityHeaterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("entity-heater-examined", ("setting", ent.Comp.Setting)));
    }

    private void OnGetVerbs(Entity<EntityHeaterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
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

    private void OnPowerChanged(Entity<EntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        // disable heating element glowing layer if theres no power
        // doesn't actually change the setting since that would be annoying
        var setting = args.Powered ? ent.Comp.Setting : EntityHeaterSetting.Off;
        _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
    }

    protected virtual void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        // Still allow changing the setting without power
        ent.Comp.Setting = setting;
        _audio.PlayPredicted(ent.Comp.SettingSound, ent, user);
        _popup.PopupClient(Loc.GetString("entity-heater-switched-setting", ("setting", setting)), ent, user);
        Dirty(ent);

        // Only show the glowing heating element layer if there's power
        if (_receiver.IsPowered(ent.Owner))
            _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
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
            EntityHeaterSetting.Low => max / 3f,
            EntityHeaterSetting.Medium => max * 2f / 3f,
            EntityHeaterSetting.High => max,
            _ => 0.01f,
        };
    }
}
