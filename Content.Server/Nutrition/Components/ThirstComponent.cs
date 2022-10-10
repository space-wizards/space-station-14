using Content.Shared.Alert;

namespace Content.Server.Nutrition.Components
{
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

    [RegisterComponent]
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
        public float CurrentThirst;

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
}
