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
}
