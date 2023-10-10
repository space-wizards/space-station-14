// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Cart;

/// <summary>
/// Raised on an entity when a cart is attaching to it (after the pulling is already done)
/// </summary>
[ByRefEvent]
public readonly struct CartAttachEvent
{
    /// <summary>
    /// The entity cart is attaching to
    /// </summary>
    public readonly EntityUid AttachTarget;

    /// <summary>
    /// Cart that is attaching
    /// </summary>
    public readonly EntityUid Attaching;

    public CartAttachEvent(EntityUid attachTarget, EntityUid attaching)
    {
        AttachTarget = attachTarget;
        Attaching = attaching;
    }
}
