using System.Threading;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// Base doafter length for uncontested breakouts.
    /// </summary>
    [DataField("baseResistTime")]
    public float BaseResistTime = 5f;

    [DataField("isEscaping")]
    public bool IsEscaping;

    public CancellationTokenSource? CancelToken;
}
