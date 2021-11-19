using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Medical.SuitSensors
{
    public class SuitSensorSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SuitSensorComponent, GetInteractionVerbsEvent>(OnVerb);
        }

        private void OnVerb(EntityUid uid, SuitSensorComponent component, GetInteractionVerbsEvent args)
        {
            // standard interaction checks
            if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            args.Verbs.UnionWith(new[]
            {
                CreateVerb(uid, component, SuitSensorMode.SensorOff),
                CreateVerb(uid, component, SuitSensorMode.SensorBinary),
                CreateVerb(uid, component, SuitSensorMode.SensorVitals),
                CreateVerb(uid, component, SuitSensorMode.SensorCords)
            });
        }

        private Verb CreateVerb(EntityUid uid, SuitSensorComponent component, SuitSensorMode mode)
        {
            return new Verb()
            {
                Text = GetModeName(mode),
                Disabled = component.Mode == mode,
                Priority = -(int) mode, // sort them in descending order
                Category = VerbCategory.SetSensor,
                Act = () => SetSensor(uid, mode, component)
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

        public void SetSensor(EntityUid uid, SuitSensorMode mode, SuitSensorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Mode = mode;
        }
    }
}
