using Content.Shared.Alert;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components
{
    [RegisterComponent, NetworkedComponent, Access(typeof(ThirstSystem))]
    public sealed class ThirstComponent : Component
    {
        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseDecayRate")]
        public float BaseDecayRate = 0.1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ActualDecayRate;

        // Thirst
        [ViewVariables(VVAccess.ReadOnly)]
        public ThirstThreshold CurrentThirstThreshold;

        public ThirstThreshold LastThirstThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("startingThirst")]
        public float CurrentThirst = -1f;

        /// <summary>
        /// The time when the hunger will update next.
        /// </summary>
        [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextUpdateTime;

        /// <summary>
        /// The time between each update.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

        [DataField("thresholds")]
        public Dictionary<ThirstThreshold, float> ThirstThresholds { get; } = new()
        {
            {ThirstThreshold.OverHydrated, 600.0f},
            {ThirstThreshold.Okay, 450.0f},
            {ThirstThreshold.Thirsty, 300.0f},
            {ThirstThreshold.Parched, 150.0f},
            {ThirstThreshold.Dead, 0.0f},
        };

        public static readonly Dictionary<ThirstThreshold, AlertType> ThirstThresholdAlertTypes = new()
        {
            {ThirstThreshold.Thirsty, AlertType.Thirsty},
            {ThirstThreshold.Parched, AlertType.Parched},
            {ThirstThreshold.Dead, AlertType.Parched},
        };
    }

    [Serializable, NetSerializable]
    public sealed class ThirstComponentState : ComponentState
    {
        public float BaseDecayRate;

        public float ActualDecayRate;

        public ThirstThreshold CurrentThirstThreshold;

        public ThirstThreshold LastThirstThreshold;

        public float CurrentThirst;

        public TimeSpan NextUpdateTime;

        public ThirstComponentState(float baseDecayRate,
            float actualDecayRate,
            ThirstThreshold currentThirstThreshold,
            ThirstThreshold lastThirstThreshold,
            float currentThirst,
            TimeSpan nextUpdateTime)
        {
            BaseDecayRate = baseDecayRate;
            ActualDecayRate = actualDecayRate;
            CurrentThirstThreshold = currentThirstThreshold;
            LastThirstThreshold = lastThirstThreshold;
            CurrentThirst = currentThirst;
            NextUpdateTime = nextUpdateTime;
        }
    }

    [Flags]
    public enum ThirstThreshold : byte
    {
        // Hydrohomies
        Dead = 0,
        Parched = 1 << 0,
        Thirsty = 1 << 1,
        Okay = 1 << 2,
        OverHydrated = 1 << 3,
    }
}
