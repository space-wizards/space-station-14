using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Singularity.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Radiation.Events;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class RadiationCollectorSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RadiationCollectorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<RadiationCollectorComponent, OnIrradiatedEvent>(OnRadiation);
        }

        private void OnInteractHand(EntityUid uid, RadiationCollectorComponent component, InteractHandEvent args)
        {
            var curTime = _gameTiming.CurTime;

            if(curTime < component.CoolDownEnd)
                return;

            ToggleCollector(uid, args.User, component);
            component.CoolDownEnd = curTime + component.Cooldown;
        }

        private void OnRadiation(EntityUid uid, RadiationCollectorComponent component, OnIrradiatedEvent args)
        {
            if (!component.Enabled) return;

            // No idea if this is even vaguely accurate to the previous logic.
            // The maths is copied from that logic even though it works differently.
            // But the previous logic would also make the radiation collectors never ever stop providing energy.
            // And since frameTime was used there, I'm assuming that this is what the intent was.
            // This still won't stop things being potentially hilariously unbalanced though.
            if (TryComp<BatteryComponent>(uid, out var batteryComponent))
            {
                var charge = args.TotalRads * component.ChargeModifier;
                batteryComponent.CurrentCharge += charge;
            }
        }

        public void ToggleCollector(EntityUid uid, EntityUid? user = null, RadiationCollectorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;
            SetCollectorEnabled(uid, !component.Enabled, user, component);
        }

        public void SetCollectorEnabled(EntityUid uid, bool enabled, EntityUid? user = null, RadiationCollectorComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;
            component.Enabled = enabled;

            // Show message to the player
            if (user != null)
            {
                var msg = component.Enabled ? "radiation-collector-component-use-on" : "radiation-collector-component-use-off";
                _popupSystem.PopupEntity(Loc.GetString(msg), uid);

            }

            // Update appearance
            UpdateAppearance(uid, component);
        }

        private void UpdateAppearance(EntityUid uid, RadiationCollectorComponent? component, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref component, ref appearance))
                return;

            var state = component.Enabled ? RadiationCollectorVisualState.Active : RadiationCollectorVisualState.Deactive;
            appearance.SetData(RadiationCollectorVisuals.VisualState, state);
        }
    }
}
