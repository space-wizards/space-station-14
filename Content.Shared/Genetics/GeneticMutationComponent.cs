using Content.Shared.FixedPoint;
using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Genetics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GeneticMutationComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> ActiveMutations = new();

    [DataField]
    public List<string> DiscoveredMutations = new();

    [DataField]
    public List<string> PotentialMutations = new();

    [DataField]
    public Dictionary<string, string> MutationSequences = new();

    [DataField]
    public Dictionary<string, string> SequenceGuesses = new();

    [DataField]
    public Dictionary<string, string> ComplementSequenceGuesses = new();

    [DataField]
    public Dictionary<string, HashSet<int>> KnownPositions = new();

    [DataField]
    public Dictionary<string, HashSet<int>> HalfKnownPositions = new();

    // Instability damage
    [DataField]
    public float UnstableDamageTimer;

    [DataField]
    public bool HasOriginalDeadThreshold;

    [DataField]
    public FixedPoint2 OriginalDeadThreshold;

    // Regeneration
    [DataField]
    public float RegenerationTimer;

    // Epilepsy
    [DataField]
    public float EpilepsyTimer;

    // Tourette
    [DataField]
    public float TouretteTimer;

    // Narcolepsy
    [DataField]
    public float NarcolepsyTimer;

    // Paranoia
    [DataField]
    public float ParanoiaTimer;

    // Random Noises
    [DataField]
    public float NoisesTimer;

    // Chameleon — track stillness timer
    [DataField]
    public float ChameleonStillTimer;

    [DataField]
    public bool ChameleonInvisible;

    [DataField]
    public Vector2 ChameleonLastPosition;

    [DataField]
    public bool HasChameleonPosition;

    // Bioluminescence entity child
    [DataField]
    public EntityUid? BioluminescenceLight;

    // XRay — track whether we added the layer
    [DataField]
    public bool XRayActive;

    // AdaptiveVision — track state
    [DataField]
    public bool AdaptiveVisionActive;

    [DataField]
    public bool AdaptiveVisionAdded;

    [DataField]
    public bool BlindnessAdded;

    [DataField]
    public bool DeafnessAdded;

    [DataField]
    public bool MuteApplied;

    [DataField]
    public bool InsulationAdded;

    [DataField]
    public bool IlliteracyAdded;

    [DataField]
    public bool StutteringAdded;

    [DataField]
    public bool MobsterAccentAdded;

    [DataField]
    public bool AllCapsAccentAdded;

    [DataField]
    public bool ScrambledAccentAdded;

    [DataField]
    public bool OwOAccentAdded;

    [DataField]
    public bool MonkeyAccentAdded;

    [DataField]
    public bool MothAccentAdded;

    [DataField]
    public bool BackwardsAccentAdded;

    [DataField]
    public bool AntiGlowActive;

    [DataField]
    public bool NearsightedAdded;

    [DataField]
    public float MinorNegativeTimer;

    [DataField]
    public Dictionary<string, float> TemporaryMutationTimers = new();

    // Scale — track what scale we applied
    [DataField]
    public float AppliedScale = 1f;

    // NoBreathing / PlasmaBreathing — track whether we modified the respirator
    [DataField]
    public bool NoBreathingApplied;

    [DataField]
    public bool PlasmaBreathingApplied;

    // Clumsiness — track whether we added the component
    [DataField]
    public bool ClumsinessApplied;

    // Telekinesis — track whether we added the tag
    [DataField]
    public bool TelekinesisApplied;

    // Telepathy — track whether we added the component
    [DataField]
    public bool TelepathyApplied;

    [DataField]
    public EntityUid? TelekinesisAction;

    [DataField]
    public EntityUid? TelepathyAction;

    [DataField]
    public EntityUid? ShockTouchAction;

    [DataField]
    public EntityUid? SpeciesShiftRevertAction;

    // AnimalHearing — track whether we modified the zoom
    [DataField]
    public bool AnimalHearingApplied;

    // SpeciesShift — track state and save original profile for restoration
    [DataField]
    public bool SpeciesShiftApplied;

    [DataField]
    public HumanoidCharacterProfile? SpeciesShiftOriginalProfile;
}
