using System;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radiation;
using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public class RadiationCollectorComponent : Component, IInteractHand, IRadiationAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private bool _enabled;
        private TimeSpan _coolDownEnd;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Collecting {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                SetAppearance(_enabled ? RadiationCollectorVisualState.Activating : RadiationCollectorVisualState.Deactivating);
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargeModifier = 30000f;

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var curTime = _gameTiming.CurTime;

            if(curTime < _coolDownEnd)
                return true;

            if (!_enabled)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("radiation-collector-component-use-on"));
                Collecting = true;
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("radiation-collector-component-use-off"));
                Collecting = false;
            }

            _coolDownEnd = curTime + TimeSpan.FromSeconds(0.81f);

            return true;
        }

        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            if (!_enabled) return;

            // No idea if this is even vaguely accurate to the previous logic.
            // The maths is copied from that logic even though it works differently.
            // But the previous logic would also make the radiation collectors never ever stop providing energy.
            // And since frameTime was used there, I'm assuming that this is what the intent was.
            // This still won't stop things being potentially hilarously unbalanced though.
            if (_entMan.TryGetComponent<BatteryComponent>(Owner, out var batteryComponent))
            {
                batteryComponent.CurrentCharge += frameTime * radiation.RadsPerSecond * ChargeModifier;
            }
        }

        protected void SetAppearance(RadiationCollectorVisualState state)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent?>(Owner, out var appearance))
            {
                appearance.SetData(RadiationCollectorVisuals.VisualState, state);
            }
        }
    }
}
