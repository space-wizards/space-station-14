using Content.Server.Power.Components;
using Content.Shared.Radiation;
using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public sealed class RadiationCollectorComponent : Component, IRadiationAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public bool Enabled;
        public TimeSpan CoolDownEnd;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Collecting {
            get => Enabled;
            set
            {
                if (Enabled == value) return;
                Enabled = value;
                SetAppearance(Enabled ? RadiationCollectorVisualState.Activating : RadiationCollectorVisualState.Deactivating);
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargeModifier = 30000f;

        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            if (!Enabled) return;

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

        public void SetAppearance(RadiationCollectorVisualState state)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<AppearanceComponent?>(Owner, out var appearance))
            {
                appearance.SetData(RadiationCollectorVisuals.VisualState, state);
            }
        }
    }
}
