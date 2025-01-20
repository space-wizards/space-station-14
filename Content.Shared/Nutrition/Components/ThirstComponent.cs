using Content.Shared.Alert;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ThirstSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ThirstComponent : Component
{
    // Base stuff
    [DataField, AutoNetworkedField]
    public float BaseDecayRate = 0.1f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ActualDecayRate;

    [DataField, AutoNetworkedField]
    public ThirstThreshold CurrentThirstThreshold;

    [DataField, AutoNetworkedField]
    public ThirstThreshold LastThirstThreshold;

    /// <summary>
    /// The thirst value as authoritatively set by the server as of <see cref="LastAuthoritativeThirstChangeTime"/>.
    /// This value should be updated relatively infrequently. To get the current thirst, which changes with each update,
    /// use <see cref="ThirstSystem.GetThirst"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float LastAuthoritativeThirstValue = -1.0f;

    /// <summary>
    /// The time at which <see cref="LastAuthoritativeThirstValue"/> was last updated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastAuthoritativeThirstChangeTime;

    /// <summary>
    /// The time when the thirst threshold will update next.
    /// </summary>
    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextThresholdUpdateTime;

    /// <summary>
    /// The time between each thirst threshold update.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ThresholdUpdateRate = TimeSpan.FromSeconds(1);

    [DataField("thresholds"), AutoNetworkedField]
    public Dictionary<ThirstThreshold, float> ThirstThresholds = new()
    {
        { ThirstThreshold.OverHydrated, 600.0f },
        { ThirstThreshold.Okay, 450.0f },
        { ThirstThreshold.Thirsty, 300.0f },
        { ThirstThreshold.Parched, 150.0f },
        { ThirstThreshold.Dead, 0.0f },
    };

    [DataField]
    public ProtoId<AlertCategoryPrototype> ThirstyCategory = "Thirst";

    public static readonly Dictionary<ThirstThreshold, ProtoId<AlertPrototype>> ThirstThresholdAlertTypes = new()
    {
        { ThirstThreshold.Thirsty, "Thirsty" },
        { ThirstThreshold.Parched, "Parched" },
        { ThirstThreshold.Dead, "Parched" },
    };
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
