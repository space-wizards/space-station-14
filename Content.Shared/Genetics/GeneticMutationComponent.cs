using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
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

    [DataField]
    public float UnstableDamageTimer;

    [DataField]
    public bool HasOriginalDeadThreshold;

    [DataField]
    public FixedPoint2 OriginalDeadThreshold;

    [DataField]
    public float RegenerationTimer;

    [DataField]
    public float EpilepsyTimer;

    [DataField]
    public float TouretteTimer;

    [DataField]
    public float NarcolepsyTimer;

    [DataField]
    public float ParanoiaTimer;

    [DataField]
    public float NoisesTimer;

    [DataField]
    public float ChameleonStillTimer;

    [DataField]
    public bool ChameleonInvisible;

    [DataField]
    public Vector2 ChameleonLastPosition;

    [DataField]
    public bool HasChameleonPosition;

    [DataField]
    public EntityUid? BioluminescenceLight;

    [DataField]
    public bool XRayActive;

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

    [DataField]
    public float AppliedScale = 1f;

    [DataField]
    public bool NoBreathingApplied;

    [DataField]
    public bool PlasmaBreathingApplied;

    [DataField]
    public bool ClumsinessApplied;

    [DataField]
    public bool TelekinesisApplied;

    [DataField]
    public bool TelepathyApplied;

    [DataField]
    public EntityUid? TelekinesisAction;

    [DataField]
    public EntityUid? TelepathyAction;

    [DataField]
    public EntityUid? ShockTouchAction;

    [DataField]
    public bool AnimalHearingApplied;

    [DataField]
    public bool MutadoneRecoveryActive;

    [DataField]
    public bool MutadoneWarningShown;

    [DataField]
    public bool HulkAddedStunImmunity;

    [DataField]
    public bool HulkEffectsApplied;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? HulkOriginalOrganProfiles;

    [DataField]
    public bool SpeciesShiftApplied;

    [DataField]
    public HumanoidCharacterProfile? SpeciesShiftOriginalProfile;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? SpeciesShiftOriginalOrganProfiles;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? SpeciesShiftOriginalMarkings;

    [DataField]
    public EntityUid? SpeciesShiftAction;
}
