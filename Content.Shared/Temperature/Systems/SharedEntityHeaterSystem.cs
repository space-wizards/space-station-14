using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
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

        var setting = (int)ent.Comp.Setting + 1;
        setting %= _settingCount;
        var nextSetting = (EntityHeaterSetting)setting;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("entity-heater-switch-setting", ("setting", nextSetting)),
            Act = () =>
            {
                ChangeSetting(ent, nextSetting, user);
                _popup.PopupClient(Loc.GetString("entity-heater-switched-setting", ("setting", nextSetting)), ent, user);
            }
        });
    }

    private void OnPowerChanged(Entity<EntityHeaterComponent> ent, ref PowerChangedEvent args)
    {
        // disable heating element glowing layer if theres no power
        // doesn't actually turn it off since that would be annoying
        var setting = args.Powered ? ent.Comp.Setting : EntityHeaterSetting.Off;
        _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
    }

    protected virtual void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        ent.Comp.Setting = setting;
        Dirty(ent, ent.Comp);
        _appearance.SetData(ent, EntityHeaterVisuals.Setting, setting);
        _audio.PlayPredicted(ent.Comp.SettingSound, ent, user);
    }

    protected float SettingPower(EntityHeaterSetting setting, float max)
    {
        return setting switch
        {
            EntityHeaterSetting.Low => max / 3f,
            EntityHeaterSetting.Medium => max * 2f / 3f,
            EntityHeaterSetting.High => max,
            _ => 0f,
        };
    }
}
