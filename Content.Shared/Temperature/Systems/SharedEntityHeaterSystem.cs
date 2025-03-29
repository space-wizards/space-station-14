using Content.Shared.Temperature.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Power;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// Handles <see cref="EntityHeaterComponent"/> updating and events.
/// </summary>
public abstract class SharedEntityHeaterSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedTemperatureSystem Temperature = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    private readonly int _settingCount = Enum.GetValues(typeof(EntityHeaterSetting)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntityHeaterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EntityHeaterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, EntityHeaterComponent comp, ref PowerChangedEvent args)
    {
        // disable heating element glowing layer if theres no power
        // doesn't actually turn it off since that would be annoying
        var setting = args.Powered
            ? comp.Setting
            : EntityHeaterSetting.Off;
        Appearance.SetData(uid, EntityHeaterVisuals.Setting, setting);
    }

    private void OnExamined(EntityUid uid, EntityHeaterComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("entity-heater-examined", ("setting", comp.Setting)));
    }

    private void OnGetVerbs(EntityUid uid, EntityHeaterComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!Timing.IsFirstTimePredicted)
        {
            return;
        }

        var setting = (int) comp.Setting;
        setting++;
        setting %= _settingCount;
        var nextSetting = (EntityHeaterSetting) setting;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("entity-heater-switch-setting", ("setting", nextSetting)),
            Act = () =>
            {
                ChangeSetting((uid, comp), nextSetting);
                Popup.PopupPredicted(Loc.GetString("entity-heater-switched-setting", ("setting", nextSetting)), uid, args.User);
            }
        });
    }

    public virtual void ChangeSetting(Entity<EntityHeaterComponent> heater, EntityHeaterSetting setting)
    {
        heater.Comp.Setting = setting;
        Dirty(heater);
    }

    protected float SettingPower(EntityHeaterSetting setting, float max)
    {
        return setting switch
        {
            EntityHeaterSetting.Low => max / 3f,
            EntityHeaterSetting.Medium => max * 2f / 3f,
            EntityHeaterSetting.High => max,
            _ => 0f
        };
    }
}
