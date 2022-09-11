using Content.Shared.Research.Prototypes;

namespace Content.Server.Lathe.Components;

/// <summary>
/// For EntityQuery to keep track of which lathes are producing
/// </summary>
[RegisterComponent]
public sealed class LatheProducingComponent : Component
{
    /// <summary>
    /// The recipe the lathe is currently producing
    /// </summary>
    [ViewVariables]
    public LatheRecipePrototype? Recipe = null;

    /// <summary>
    /// Remaining production time, in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeRemaining;
}

