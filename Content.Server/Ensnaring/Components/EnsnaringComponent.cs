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

/// <summary>
/// Used for the do after event to free the entity that owns the <see cref="EnsnareableComponent"/>
/// </summary>
public sealed class FreeEnsnareDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid EnsnaringEntity;

    public FreeEnsnareDoAfterComplete(EntityUid ensnaringEntity)
    {
        EnsnaringEntity = ensnaringEntity;
    }
}

/// <summary>
/// Used for the do after event when it fails to free the entity that owns the <see cref="EnsnareableComponent"/>
/// </summary>
public sealed class FreeEnsnareDoAfterCancel : EntityEventArgs
{
    public readonly EntityUid EnsnaringEntity;

    public FreeEnsnareDoAfterCancel(EntityUid ensnaringEntity)
    {
        EnsnaringEntity = ensnaringEntity;
    }
}
