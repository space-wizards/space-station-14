using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Add to an entity to paralyze it whenever it reaches critical amounts of Stamina DamageType.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class StaminaComponent : Component
{
    /// <summary>
    /// Have we reached peak stamina damage and been paralyzed?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Critical;

    /// <summary>
    /// How much stamina reduces per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Decay = 3f;

    /// <summary>
    /// How much time after receiving damage until stamina starts decreasing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Cooldown = 3f;

    /// <summary>
    /// How much stamina damage this entity has taken.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float StaminaDamage;

    /// <summary>
    /// The base stamina the entity requires to enter stam crit. Should rarely if ever be modified outside of yaml.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseCritThreshold = 100f;

    /// <summary>
    /// Modified crit threshold for when an entity should enter stamcrit.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float CritThreshold = 100f;

    /// <summary>
    /// How long will this mob be stunned for?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(6);

    /// <summary>
    /// To avoid continuously updating our data we track the last time we updated so we can extrapolate our current stamina.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public ProtoId<AlertPrototype> StaminaAlert = "Stamina";

    /// <summary>
    /// This flag indicates whether the value of <see cref="StaminaDamage"/> decreases after the entity exits stamina crit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AfterCritical;

    /// <summary>
    /// This float determines how fast stamina will regenerate after exiting the stamina crit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AfterCritDecayMultiplier = 5f;

    /// <summary>
    /// This is how much stamina damage a mob takes when it forces itself to stand up before modifiers
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ForceStandStamina = 10f;

    /// <summary>
    /// What sound should play when we successfully stand up
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ForceStandSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    /// <summary>
    /// Thresholds that determine an entity's slowdown as a function of stamina damage, in percentages.
    /// </summary>
    [DataField]
    public Dictionary<FixedPoint2, float> StunModifierThresholds = new() { {0, 1f }, { 0.6, 0.7f }, { 0.8, 0.5f } };

    #region Animation Data

    /// <summary>
    /// Threshold at which low stamina animations begin playing. This should be set to a value that means something.
    /// At 50, it is aligned so when you hit 60 stun the entity will be breathing once per second (well above hyperventilation).
    /// </summary>
    [DataField]
    public float AnimationThreshold = 50;

    /// <summary>
    /// Minimum y vector displacement for breathing at AnimationThreshold
    /// </summary>
    [DataField]
    public float BreathingAmplitudeMin = 0.04f;

    /// <summary>
    /// Maximum y vector amount we add to the BreathingAmplitudeMin
    /// </summary>
    [DataField]
    public float BreathingAmplitudeMod = 0.04f;

    /// <summary>
    /// Minimum vector displacement for jittering at AnimationThreshold
    /// </summary>
    [DataField]
    public float JitterAmplitudeMin;

    /// <summary>
    /// Maximum vector amount we add to the JitterAmplitudeMin
    /// </summary>
    [DataField]
    public float JitterAmplitudeMod = 0.04f;

    /// <summary>
    /// Min multipliers for JitterAmplitude in the X and Y directions, animation randomly chooses between these min and max multipliers
    /// </summary>
    [DataField]
    public Vector2 JitterMin = Vector2.Create(0.5f, 0.125f);

    /// <summary>
    /// Max multipliers for JitterAmplitude in the X and Y directions, animation randomly chooses between these min and max multipliers
    /// </summary>
    [DataField]
    public Vector2 JitterMax = Vector2.Create(1f, 0.25f);

    /// <summary>
    /// Minimum total animations per second
    /// </summary>
    [DataField]
    public float FrequencyMin = 0.25f;

    /// <summary>
    /// Maximum amount we add to the Frequency min just before crit
    /// </summary>
    [DataField]
    public float FrequencyMod = 1.75f;

    /// <summary>
    /// Jitter keyframes per animation
    /// </summary>
    [DataField]
    public int Jitters = 4;

    /// <summary>
    /// Vector of the last Jitter so we can make sure we don't jitter in the same quadrant twice in a row.
    /// </summary>
    [DataField]
    public Vector2 LastJitter;

    /// <summary>
    ///     The offset that an entity had before jittering started,
    ///     so that we can reset it properly.
    /// </summary>
    [DataField]
    public Vector2 StartOffset = Vector2.Zero;

    #endregion
}
