namespace Content.Server.Traits.Assorted;

/// <summary>
/// This trait prevents the owner's brain from being inserted into an MMI.
/// All this component does is add the <see cref="BrainUnborgableComponent"/> to the brain.
/// </summary>
[RegisterComponent, Access(typeof(UnborgableSystem))]
public sealed partial class UnborgableComponent : Component
{
    /// <summary>
    /// The message that will be disaplyed when a player tries to insert the owner's brain in an MMI.
    /// </summary>
    [DataField]
    public string FailureMessage = "error-brain-incompatible-mmi";
}
