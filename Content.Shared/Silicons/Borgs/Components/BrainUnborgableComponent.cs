using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This component makes the entity impossible to insert into an MMI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BrainUnborgableComponent : Component
{
    /// <summary>
    /// The message that will be displayed when a player tries to insert the brain in an MMI.
    /// </summary>
    [DataField]
    public string FailureMessage = "error-brain-incompatible-mmi";
}
