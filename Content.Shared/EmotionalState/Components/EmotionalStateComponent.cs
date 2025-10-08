using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.EmotionalState;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class EmotionalStateComponent : Component
{
    /// <summary>
    /// Field for storing emotional state points.
    /// </summary>
    [DataField]
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float LastAuthoritativeEmotionalValue;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public TimeSpan LastAuthoritativeEmotionalChangeTime;

    /// <summary>
    /// The magnitude of the last change in emotional state points.
    /// Needed for the correct functioning of icons (visor)
    /// </summary>
    [DataField]
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public float LastDeltaAuthoritativeEmotionalValue;

    /// <summary>
    /// Dictionary containing the prototypes of creatures that somehow affect the emotional state.
    /// </summary>
    [DataField("triggers"), ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<string, float[]> Triggers = new Dictionary<string, float[]>();

    /// <summary>
    /// Dictionary containing the prototypes of substances that somehow affect the emotional state.
    /// The positive effect from them is applied immediately, while the negative effect is applied over time.
    /// </summary>
    [DataField("triggersReagent"), ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<string, string> TriggersReagent = new Dictionary<string, string>();

    /// <summary>
    /// List for negative effects from reagents in the organism.
    /// Each NegativeEffects element contains a value that needs
    /// to be applied every second, and the duration for which this value
    /// will be applied. Everything is calculated in <see cref="EmotionalStateSystem.UpdateCurrentThreshold()"/>
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public List<List<float>> NegativeEffects = new List<List<float>>();

    /// <summary>
    /// The probability that a humanoid will commit suicide when in a depressive (or lower) state.
    /// </summary>
    [DataField("chanceOfSuicide"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float ChanceOfSuicide = 0.15f;

    /// <summary>
    /// Theoretically, it might turn out that the player's current role is excluded by all triggers. Fearless :)
    /// Not sure if this should be kept... Because there should be many triggers, and only syndicate and Central Command roles
    /// could be excluded from all of them. And leaving them the ability to lose emotional state points through
    /// taking damage or reagents... Might not be the best idea :)
    /// </summary>
    [DataField("fearless"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Fearless = false;

    /// <summary>
    /// The last threshold this entity was at.
    /// Stored in order to prevent recalculating
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EmotionalThreshold LastThreshold;

    /// <summary>
    /// Radius within which visible objects will
    /// somehow affect the humanoid's emotional state.
    /// </summary>
    [DataField("rangeTrigger"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RangeTrigger = 5;

    /// <summary>
    /// The current emotional state threshold
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EmotionalThreshold CurrentThreshold;

    /// <summary>
    /// Dictionary defining the boundaries of each emotional state.
    /// Used in <see cref="EmotionalStateSystem.GetEmotionalStateThreshold"/>
    /// </summary>
    [DataField("thresholds", customTypeSerializer: typeof(DictionarySerializer<EmotionalThreshold, float>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, float> Thresholds = new()
    {
        { EmotionalThreshold.Rainbow, 1000.0f },
        { EmotionalThreshold.Excellent, 900.0f },
        { EmotionalThreshold.Good, 750.0f },
        { EmotionalThreshold.Neutral, 600.0f },
        { EmotionalThreshold.Sad, 400.0f },
        { EmotionalThreshold.Depressive, 200.0f },
        { EmotionalThreshold.Demonic, 0.0f }
    };

    /// <summary>
    /// Dictionary storing prototype IDs of icons for the current emotional state.
    /// </summary>
    [DataField("statusIconsThresholds", customTypeSerializer: typeof(DictionarySerializer<EmotionalThreshold, string>)), ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, string> StatusIconsThresholds = new()
    {
        { EmotionalThreshold.Rainbow, "EmotionalStateIconRainbow" },
        { EmotionalThreshold.Excellent, "EmotionalStateIconExcellent" },
        { EmotionalThreshold.Good, "EmotionalStateIconGood" },
        { EmotionalThreshold.Neutral, "EmotionalStateIconNeutral" },
        { EmotionalThreshold.Sad, "EmotionalStateIconSad" },
        { EmotionalThreshold.Depressive, "EmotionalStateIconDepressive" },
        { EmotionalThreshold.Demonic, "EmotionalStateIconDemonic" }
    };

    /// <summary>
    /// Dictionary containing prototype IDs of the icon (alert) for the current emotional state
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, ProtoId<AlertPrototype>> EmotionalThresholdAlerts = new()
    {
        { EmotionalThreshold.Rainbow, "Rainbow" },
        { EmotionalThreshold.Excellent, "Excellent" },
        { EmotionalThreshold.Good, "Good" },
        { EmotionalThreshold.Neutral, "Neutral" },
        { EmotionalThreshold.Sad, "Sad" },
        { EmotionalThreshold.Depressive, "Depressive" },
        { EmotionalThreshold.Demonic, "Demonic" }
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<AlertCategoryPrototype> EmotionalAlertCategory = "Emotions";

    /// <summary>
    /// Dictionary containing modifiers that are applied to state points gain.
    /// In a good state, we pay less attention to failures/triggers.
    /// </summary>
    [DataField("emotionalPositiveDecayModifiers", customTypeSerializer: typeof(DictionarySerializer<EmotionalThreshold, float>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, float> EmotionalPositiveDecayModifiers = new()
    {
        { EmotionalThreshold.Rainbow, 1.5f },
        { EmotionalThreshold.Excellent, 1.3f },
        { EmotionalThreshold.Good, 1.1f },
        { EmotionalThreshold.Neutral, 1.0f },
        { EmotionalThreshold.Sad, 0.8f },
        { EmotionalThreshold.Depressive, 0.6f },
        { EmotionalThreshold.Demonic, 0.2f }
    };

    /// <summary>
    /// Dictionary containing modifiers that are applied to state points loss.
    /// In a bad state, we are more sensitive to negative emotions.
    /// </summary>
    [DataField("emotionalNegativeDecayModifiers", customTypeSerializer: typeof(DictionarySerializer<EmotionalThreshold, float>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, float> EmotionalNegativeDecayModifiers = new()
    {
        { EmotionalThreshold.Rainbow, 0.2f },
        { EmotionalThreshold.Excellent, 0.6f },
        { EmotionalThreshold.Good, 0.8f },
        { EmotionalThreshold.Neutral, 1.0f },
        { EmotionalThreshold.Sad, 1.1f },
        { EmotionalThreshold.Depressive, 1.3f },
        { EmotionalThreshold.Demonic, 1.5f }
    };

    /// <summary>
    /// Dictionary of speed modifiers for different emotional states.
    /// </summary>
    [DataField("speedModifer", customTypeSerializer: typeof(DictionarySerializer<EmotionalThreshold, float>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<EmotionalThreshold, float> SpeedModifer = new()
    {
        { EmotionalThreshold.Rainbow, 1.25f },
        { EmotionalThreshold.Excellent, 1.05f },
        { EmotionalThreshold.Good, 1.0f },
        { EmotionalThreshold.Neutral, 1.0f },
        { EmotionalThreshold.Sad, 0.9f },
        { EmotionalThreshold.Depressive, 0.85f },
        { EmotionalThreshold.Demonic, 0.8f }
    };

    /// <summary>
    /// The time when the hunger threshold will update next.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextThresholdUpdateTime;

    /// <summary>
    /// The time between each threshold update.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan ThresholdUpdateRate = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public enum EmotionalThreshold : byte
{
    Rainbow = 1 << 5,
    Excellent = 1 << 4,
    Good = 1 << 3,
    Neutral = 1 << 2,
    Sad = 1 << 1,
    Depressive = 1 << 0,
    Demonic = 0,
}
