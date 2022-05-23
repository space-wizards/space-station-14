using System.Threading;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// How long it takes to break out of storage. Default at 5 seconds.
    /// </summary>
    [ViewVariables]
    [DataField("resistTime")]
    public float ResistTime = 5f;

    /// <summary>
    /// For quick exit if the player attempts to move while already resisting
    /// </summary>
    [ViewVariables]
    public bool IsResisting = false;

    /// <summary>
    /// Cancellation token used to cancel the DoAfter if the mob is removed before it's complete
    /// </summary>
    public CancellationTokenSource? CancelToken;
}
