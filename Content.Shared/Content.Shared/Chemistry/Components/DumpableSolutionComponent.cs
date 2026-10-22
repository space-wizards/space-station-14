using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Denotes that there is a solution contained in this entity that can be
/// easily dumped into (that is, completely removed from the dumping container
/// into this one). Think pouring a container fully into this. The action for this is represented via drag & drop.
///
/// To represent it being possible to controllably pour volumes into the entity, see <see cref="RefillableSolutionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DumpableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be dumped into.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// Whether the solution can be dumped into infinitely.
    /// </summary>
    /// <remarks>Note that this is what makes the ChemMaster's buffer a stasis buffer as well!</remarks>
    [DataField]
    public bool Unlimited = false;
}
