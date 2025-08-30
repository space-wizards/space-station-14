using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This trait prevents the owner's brain from being inserted into an MMI.
/// All this component does is add the <see cref="MMIIncompatibleComponent"/> to the brain.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnborgableComponent : Component
{
    /// <summary>
    /// The message that will be disaplyed when a player tries to insert the owner's brain in an MMI.
    /// </summary>
    [DataField]
    public LocId FailureMessage = "error-brain-incompatible-mmi";
}
