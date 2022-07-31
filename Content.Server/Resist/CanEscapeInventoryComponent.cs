using System.Threading;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// Base doafter length for uncontested breakouts.
    /// </summary>
    [ViewVariables]
    [DataField("baseResistTime")]
    public float BaseResistTime = 5f;

    /// <summary>
    /// Cancellation token used to cancel the DoAfter if the mob is removed before it's complete
    /// </summary>
    public CancellationTokenSource? CancelToken;
}
