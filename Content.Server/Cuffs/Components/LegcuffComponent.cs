using System.Threading;
using Content.Shared.Cuffs.Components;

namespace Content.Server.Cuffs.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedLegcuffComponent))]
public sealed class LegcuffComponent : SharedLegcuffComponent
{
    /// <summary>
    /// Should movement cancel breaking out of the legcuffs?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canMoveBreakout")]
    public bool CanMoveBreakout;

    public CancellationTokenSource? CancelToken = null;
}
