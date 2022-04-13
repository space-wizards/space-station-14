using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Analyzers;
using System.Threading;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed class CanResistInventoryComponent : Component
{
    /// <summary>
    /// How long it takes to break out of storage. Default at 20 seconds.
    /// </summary>
    [ViewVariables]
    [DataField("resistTime")]
    public float ResistTime = 20f;

    /// <summary>
    /// Cancellation token used to cancel the DoAfter if the mob is removed before it's complete
    /// </summary>
    public CancellationTokenSource? CancelToken;
}
