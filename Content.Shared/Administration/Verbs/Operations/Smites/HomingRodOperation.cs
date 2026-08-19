using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

public sealed partial class HomingRodOperation : AdminOperationBase<HomingRodOperation>
{
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    [DataField(required: true)]
    public float Distance { get; private set; }

    [DataField(required: true)]
    public float Speed { get; private set; }

    /// <summary>
    /// Uses the target's sprint speed when available, falling back to <see cref="Speed"/>.
    /// </summary>
    [DataField]
    public bool MatchTargetSprintSpeed { get; private set; }
}
