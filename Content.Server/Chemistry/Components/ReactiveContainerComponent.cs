using Content.Server.Chemistry.EntitySystems;
namespace Content.Server.Chemistry.Components;

/// <summary>
///     Represents a container that also contains a solution.
///     This means that reactive entities react when inserted into the container.
/// </summary>
[RegisterComponent]
public sealed partial class ReactiveContainerComponent : Component
{
    /// <summary>
    ///     The container that holds the solution.
    /// </summary>
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    /// <summary>
    ///     The solution in the container.
    /// </summary>
    [DataField("solution", required: true)]
    public string Solution = default!;
}
