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
}
