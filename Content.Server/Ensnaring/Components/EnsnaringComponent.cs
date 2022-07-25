using System.Threading;
using Content.Shared.Ensnaring.Components;

namespace Content.Server.Ensnaring.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedEnsnaringComponent))]
public sealed class EnsnaringComponent : SharedEnsnaringComponent
{
    /// <summary>
    /// Should movement cancel breaking out?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canMoveBreakout")]
    public bool CanMoveBreakout;

    public CancellationTokenSource? CancelToken;
}
