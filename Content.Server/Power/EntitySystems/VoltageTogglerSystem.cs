using Content.Server.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Power.EntitySystems;

public sealed class VoltageTogglerSystem : SharedVoltageTogglerSystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = null!;
    [Dependency] private readonly PopupSystem _popup = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoltageTogglerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VoltageTogglerComponent> entity, ref MapInitEvent args)
    {
        ChangeVoltage(entity, entity.Comp.SelectedVoltageLevel, null);
    }

    protected override void ChangeVoltage(Entity<VoltageTogglerComponent> entity, int settingIndex, EntityUid? user)
    {
        // no sound spamming
        if (!TryComp<UseDelayComponent>(entity, out var useDelay) || _useDelay.IsDelayed((entity, useDelay)))
            return;

        entity.Comp.SelectedVoltageLevel = settingIndex;
        var setting = entity.Comp.Settings[settingIndex];

        Dirty(entity);

        var ev = new VoltageChangedEvent(setting);
        RaiseLocalEvent(entity, ref ev);

        _useDelay.TryResetDelay((entity, useDelay));

        if (user == null)
            return;

        var voltage = setting.Voltage;
        var popup = Loc.GetString(entity.Comp.SwitchText, ("voltage", VoltageString(voltage)));
        _popup.PopupEntity(popup, entity, user.Value);

        _audio.PlayPvs(entity.Comp.SwitchSound, entity);
    }
}
