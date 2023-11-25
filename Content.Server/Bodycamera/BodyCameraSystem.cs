using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;
using Content.Shared.Timing;

namespace Content.Server.Bodycamera
{
    public sealed class BodyCameraSystem : EntitySystem
    {
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<BodyCameraComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<BodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<BodyCameraComponent, ExaminedEvent>(OnExamine);
        }

        private void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
        {
            if (TryComp<SurveillanceCameraComponent>(uid, out SurveillanceCameraComponent? comp))
            {
                _camera.SetActive(uid, false, comp);
            }
        }
        private void OnPowerCellChanged(EntityUid uid, BodyCameraComponent comp, PowerCellChangedEvent args)
        {
            if (args.Ejected)
                TryDisable(uid, comp);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BodyCameraComponent>();
            while (query.MoveNext(out var uid, out var cam))
            {
                if (cam.Enabled && !_powerCell.TryUseCharge(uid, cam.Wattage * frameTime))
                {
                    TryDisable(uid, cam);
                }
            }
        }

        private void OnActivate(EntityUid uid, BodyCameraComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || _useDelay.ActiveDelay(uid))
                return;

            if (!TryToggle(uid, component, args.User))
                return;

            args.Handled = true;
            _useDelay.BeginDelay(uid);
            var state = Loc.GetString(component.Enabled ? "bodycamera-component-on-state" : "bodycamera-component-off-state");
            var message = Loc.GetString("bodycamera-component-on-use", ("state", state));
            _popup.PopupEntity(message, args.User, args.User);
            args.Handled = true;
        }

        private void OnExamine(EntityUid uid, BodyCameraComponent comp, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                var msg = comp.Enabled
                    ? Loc.GetString("bodycamera-component-examine-on-state")
                    : Loc.GetString("bodycamera-component-examine-off-state");
                args.PushMarkup(msg);
            }
        }

        private bool TryToggle(EntityUid uid, BodyCameraComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            return component.Enabled
                ? TryDisable(uid, component)
                : TryEnable(uid, component, user);
        }

        private bool TryEnable(EntityUid uid, BodyCameraComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.Enabled)
                return false;

            if (!_powerCell.TryUseCharge(uid, component.Wattage))
                return false;

            if (TryComp<SurveillanceCameraComponent>(uid, out SurveillanceCameraComponent? comp))
            {
                _camera.SetActive(uid, true, comp);
            }

            component.Enabled = true;
            _audio.PlayPvs(component.PowerOnSound, uid);
            return true;
        }

        private bool TryDisable(EntityUid uid, BodyCameraComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!component.Enabled)
                return false;

            if (TryComp<SurveillanceCameraComponent>(uid, out SurveillanceCameraComponent? comp))
            {
                _camera.SetActive(uid, false, comp);
            }

            component.Enabled = false;
            _audio.PlayPvs(component.PowerOffSound, uid);
            return true;
        }
    }
}
