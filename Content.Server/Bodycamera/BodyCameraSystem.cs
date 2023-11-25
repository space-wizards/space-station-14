using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.SurveillanceCamera;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;

namespace Content.Server.Bodycamera
{
    public sealed class BodyCameraSystem : EntitySystem
    {
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SurveillanceCameraSystem _camera = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyCameraComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
        {
            if (TryComp<SurveillanceCameraComponent>(uid, out SurveillanceCameraComponent? comp))
            {
                _camera.SetActive(uid, false, comp);
            }
        }
        private void OnUseInHand(EntityUid uid, BodyCameraComponent component, UseInHandEvent args)
        {
            if (args.Handled || _useDelay.ActiveDelay(uid))
                return;

            if (!TryToggle(uid, component, args.User))
                return;

            args.Handled = true;
            _useDelay.BeginDelay(uid);
        }

        public bool TryToggle(EntityUid uid, BodyCameraComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            return component.Enabled
                ? TryDisable(uid, component)
                : TryEnable(uid, component, user);
        }

        public bool TryEnable(EntityUid uid, BodyCameraComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.Enabled)
                return false;

            if (TryComp<SurveillanceCameraComponent>(uid, out SurveillanceCameraComponent? comp))
            {
                _camera.SetActive(uid, true, comp);
            }

            component.Enabled = true;
            _audio.PlayPvs(component.PowerOnSound, uid);
            return true;
        }

        public bool TryDisable(EntityUid uid, BodyCameraComponent? component = null)
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
