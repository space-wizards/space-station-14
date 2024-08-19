using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Examine;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Robust.Server.Audio;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles <see cref="EntityHeaterComponent"/> updating and events.
/// </summary>
public sealed class EntityHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private readonly int SettingCount = Enum.GetValues(typeof(EntityHeaterSetting)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntityHeaterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EntityHeaterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<EntityHeaterComponent, ItemPlacerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var comp, out var placer, out var power))
        {
            if (!power.Powered)
                continue;

            // don't divide by total entities since its a big grill
            // excess would just be wasted in the air but that's not worth simulating
            // if you want a heater thermomachine just use that...
            var energy = power.PowerReceived * deltaTime;
            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, energy);
            }
        }
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

        var setting = (int) comp.Setting;
        setting++;
        setting %= SettingCount;
        var nextSetting = (EntityHeaterSetting) setting;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("entity-heater-switch-setting", ("setting", nextSetting)),
            Act = () =>
            {
                ChangeSetting(uid, nextSetting, comp);
                _popup.PopupEntity(Loc.GetString("entity-heater-switched-setting", ("setting", nextSetting)), uid, args.User);
            }
        });
    }

    private void OnPowerChanged(EntityUid uid, EntityHeaterComponent comp, ref PowerChangedEvent args)
    {
        // disable heating element glowing layer if theres no power
        // doesn't actually turn it off since that would be annoying
        var setting = args.Powered ? comp.Setting : EntityHeaterSetting.Off;
        _appearance.SetData(uid, EntityHeaterVisuals.Setting, setting);
    }

    private void ChangeSetting(EntityUid uid, EntityHeaterSetting setting, EntityHeaterComponent? comp = null, ApcPowerReceiverComponent? power = null)
    {
        if (!Resolve(uid, ref comp, ref power))
            return;

        comp.Setting = setting;
        power.Load = SettingPower(setting, comp.Power);
        _appearance.SetData(uid, EntityHeaterVisuals.Setting, setting);
        _audio.PlayPvs(comp.SettingSound, uid);
    }

    private float SettingPower(EntityHeaterSetting setting, float max)
    {
        switch (setting)
        {
            case EntityHeaterSetting.Low:
                return max / 3f;
            case EntityHeaterSetting.Medium:
                return max * 2f / 3f;
            case EntityHeaterSetting.High:
                return max;
            default:
                return 0f;
        }
    }
}
