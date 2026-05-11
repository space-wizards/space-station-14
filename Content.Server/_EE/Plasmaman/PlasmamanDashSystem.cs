using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._EE.Plasmaman;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;

namespace Content.Server._EE.Plasmaman;

public sealed class PlasmamanDashSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmamanDashComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlasmamanDashComponent, PlasmamanDashEvent>(OnDash);
    }

    private void OnMapInit(Entity<PlasmamanDashComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnDash(Entity<PlasmamanDashComponent> ent, ref PlasmamanDashEvent args)
    {
        if (args.Handled)
            return;

        var mixture = _atmosphere.GetContainingMixture(ent.Owner, excite: true);
        if (mixture != null && mixture.TotalMoles > ent.Comp.MaxAtmosMoles)
        {
            _popup.PopupEntity(Loc.GetString("plasmaman-dash-no-vacuum"), ent, ent);
            return;
        }

        if (!TryComp<RespiratorComponent>(ent, out var respirator) ||
            respirator.Saturation < ent.Comp.MinSaturation)
        {
            _popup.PopupEntity(Loc.GetString("plasmaman-dash-no-breath"), ent, ent);
            return;
        }

        var ratio = Math.Clamp(respirator.Saturation / respirator.MaxSaturation, 0f, 1f);
        var strength = float.Lerp(ent.Comp.MinStrength, ent.Comp.MaxStrength, ratio);

        var rotation = _transform.GetWorldRotation(ent);
        var direction = rotation.ToWorldVec() * strength;

        _throwing.TryThrow(
            ent,
            direction,
            baseThrowSpeed: strength,
            user: ent,
            pushbackRatio: 0f,
            animated: false,
            playSound: false,
            doSpin: false);

        _respirator.UpdateSaturation(ent, -ent.Comp.SaturationCost, respirator);

        if (ent.Comp.Sound != null)
            _audio.PlayPredicted(ent.Comp.Sound, ent, ent);

        args.Handled = true;
    }
}
