using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.PowerCell;
using Content.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Server.Bodycamera
{
    public sealed class BodyCameraSystem : EntitySystem
    {
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<BodyCameraComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
            SubscribeLocalEvent<BodyCameraComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<BodyCameraComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BodyCameraComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
        {
            //Cameras start enabled, this disables the camera to match the BodyCamera's initial state
            if (TryComp<SurveillanceCameraComponent>(uid, out var comp))
            {
                _camera.SetActive(uid, false, comp);
            }
        }

        private void OnPowerCellSlotEmpty(EntityUid uid, BodyCameraComponent component, ref PowerCellSlotEmptyEvent args)
        {
            if (component.Enabled)
                TryDisable(uid, component);
        }

        private void OnEquipped(EntityUid uid, BodyCameraComponent component, GotEquippedEvent args)
        {
            //Disable the camera if placed in a pocket
            if (TryComp<ClothingComponent>(uid, out var clothingComp) && (clothingComp.Slots & args.SlotFlags) != args.SlotFlags)
            {
                return;
            }

            if (!TryEnable(uid, component))
                return;

            component.Equipped = true;
            if (TryComp<SurveillanceCameraComponent>(uid, out var camera)
                && _idCardSystem.TryFindIdCard(args.Equipee, out var card))
            {
                var userName = Loc.GetString("bodycamera-component-unknown-name");
                var userJob = Loc.GetString("bodycamera-component-unknown-job");

                if (card.Comp.FullName != null)
                    userName = card.Comp.FullName;
                if (card.Comp.JobTitle != null)
                    userJob = card.Comp.JobTitle;

                string cameraName = $"{userJob} - {userName}";
                _camera.SetName(uid, cameraName, camera);
            }

            var state = Loc.GetString(component.Enabled ? "bodycamera-component-on-state" : "bodycamera-component-off-state");
            var message = Loc.GetString("bodycamera-component-on-use", ("state", state));
            _popup.PopupEntity(message, args.Equipee);
        }

        private void OnUnequipped(EntityUid uid, BodyCameraComponent component, GotUnequippedEvent args)
        {
            if (!TryDisable(uid, component))
                return;

            component.Equipped = false;
            var state = Loc.GetString(component.Enabled ? "bodycamera-component-on-state" : "bodycamera-component-off-state");
            var message = Loc.GetString("bodycamera-component-on-use", ("state", state));
            _popup.PopupEntity(message, args.Equipee);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BodyCameraComponent>();
            while (query.MoveNext(out var uid, out var cam))
            {
                if (cam.Enabled)
                {
                    if (!_powerCell.TryUseCharge(uid, cam.Wattage * frameTime)) TryDisable(uid, cam);
                }
            }
        }

        private void OnExamine(EntityUid uid, BodyCameraComponent comp, ExaminedEvent args)
        {
            var msg = comp.Enabled
                ? Loc.GetString("bodycamera-component-examine-on-state")
                : Loc.GetString("bodycamera-component-examine-off-state");
            args.PushMarkup(msg);
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
