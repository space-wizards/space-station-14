using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics;

[RegisterComponent]
public sealed partial class GeneticsConsoleComponent : Component
{
    public const string ScannerPort = "GeneticsScannerSender";

    [ViewVariables]
    public EntityUid? GeneticScanner;

    [DataField("maxDistance")]
    public float MaxDistance = 4f;

    [DataField]
    public List<string> MemoryMutations = new();

    [DataField]
    public int MaxMemorySlots = 10;

    public string? LastResult;

    public bool GeneticScannerInRange = true;

    [DataField]
    public float JokerCooldownRemaining;

    [DataField]
    public string? LastJokerMutationId;

    [DataField]
    public HumanoidCharacterProfile? StoredEnzymeProfile;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? StoredOrganProfiles;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? StoredMarkings;
}
