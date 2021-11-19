using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Medical.SuitSensors
{
    public class SuitSensorSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SuitSensorComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SuitSensorComponent, EquippedEvent>(OnEquipped);
            SubscribeLocalEvent<SuitSensorComponent, UnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<SuitSensorComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<SuitSensorComponent, GetInteractionVerbsEvent>(OnVerb);
        }

        private void OnMapInit(EntityUid uid, SuitSensorComponent component, MapInitEvent args)
        {
            if (!component.RandomMode)
                return;

            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                SuitSensorMode.SensorOff,
                SuitSensorMode.SensorBinary, SuitSensorMode.SensorBinary,
                SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals,
                SuitSensorMode.SensorCords, SuitSensorMode.SensorCords
            };
            component.Mode = _random.Pick(modesDist);
        }

        private void OnEquipped(EntityUid uid, SuitSensorComponent component, EquippedEvent args)
        {
            if (args.Slot != component.ActivationSlot)
                return;

            component.User = args.User.Uid;
        }

        private void OnUnequipped(EntityUid uid, SuitSensorComponent component, UnequippedEvent args)
        {
            if (args.Slot != component.ActivationSlot)
                return;

            component.User = null;
        }

        private void OnExamine(EntityUid uid, SuitSensorComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            string msg;
            switch (component.Mode)
            {
                case SuitSensorMode.SensorOff:
                    msg = "suit-sensor-examine-off";
                    break;
                case SuitSensorMode.SensorBinary:
                    msg = "suit-sensor-examine-binary";
                    break;
                case SuitSensorMode.SensorVitals:
                    msg = "suit-sensor-examine-vitals";
                    break;
                case SuitSensorMode.SensorCords:
                    msg = "suit-sensor-examine-cords";
                    break;
                default:
                    return;
            }

            args.PushMarkup(Loc.GetString(msg));
        }

        private void OnVerb(EntityUid uid, SuitSensorComponent component, GetInteractionVerbsEvent args)
        {
            // standard interaction checks
            if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            args.Verbs.UnionWith(new[]
            {
                CreateVerb(uid, component, args.User.Uid, SuitSensorMode.SensorOff),
                CreateVerb(uid, component, args.User.Uid, SuitSensorMode.SensorBinary),
                CreateVerb(uid, component, args.User.Uid,SuitSensorMode.SensorVitals),
                CreateVerb(uid, component, args.User.Uid, SuitSensorMode.SensorCords)
            });
        }

        private Verb CreateVerb(EntityUid uid, SuitSensorComponent component, EntityUid userUid, SuitSensorMode mode)
        {
            return new Verb()
            {
                Text = GetModeName(mode),
                Disabled = component.Mode == mode,
                Priority = -(int) mode, // sort them in descending order
                Category = VerbCategory.SetSensor,
                Act = () => SetSensor(uid, mode, userUid, component)
            };
        }

        private string GetModeName(SuitSensorMode mode)
        {
            string name;
            switch (mode)
            {
                case SuitSensorMode.SensorOff:
                    name = "suit-sensor-mode-off";
                    break;
                case SuitSensorMode.SensorBinary:
                    name = "suit-sensor-mode-binary";
                    break;
                case SuitSensorMode.SensorVitals:
                    name = "suit-sensor-mode-vitals";
                    break;
                case SuitSensorMode.SensorCords:
                    name = "suit-sensor-mode-cords";
                    break;
                default:
                    return "";
            }

            return Loc.GetString(name);
        }

        public void SetSensor(EntityUid uid, SuitSensorMode mode, EntityUid? userUid = null,
            SuitSensorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Mode = mode;

            if (userUid != null)
            {
                var msg = Loc.GetString("suit-sensor-mode-state", ("mode", GetModeName(mode)));
                _popupSystem.PopupEntity(msg, uid, Filter.Entities(userUid.Value));
            }
        }
    }
}
