using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class TrashVariationPassComponent : Component
{
    /// <summary>
    ///     Number of tiles before we spawn one trash on average.
    /// </summary>
    [DataField]
    public float TilesPerTrashAverage = 50f;

    [DataField]
    public float TilesPerTrashStdDev = 7f;

    /// <summary>
    ///     Entity to spawn to use to spawn trash (rather than just doing it ourselves)
    /// </summary>
    /// <remarks>
    ///     its really just called "RandomSpawner"?
    /// </remarks>
    public EntProtoId TrashSpawner = "RandomSpawner";
}
