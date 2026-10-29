namespace Content.Shared.SolutionAppearance;

/// <summary>
/// Allows the visuals of an inserted solution to be relayed to the owner of this component. Using <see cref="SolutionItemSlotAppearanceSystem" />
/// </summary>
[RegisterComponent]
public sealed partial class SolutionItemSlotAppearanceComponent : Component
{
    /// <summary>
    /// Which container ID to check for the solution.
    /// </summary>
    [DataField(required: true)]
    public string ContainerID = default!;
}

/// <summary>
/// Raised directed on a container owner to check if the given container ID is valid for relaying solution visuals.
/// </summary>
[ByRefEvent]
public record struct IsValidSolutionContainerEvent(string ContainerId, bool IsValid = false);
