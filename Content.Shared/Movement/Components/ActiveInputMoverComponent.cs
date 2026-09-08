using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Marker component for entities that are being processed by MoverController.
/// </summary>
/// <remarks>
/// The idea here is to keep track via event subscriptions which mover
/// controllers actually need to be processed. Instead of having this be a
/// boolean field on the <see cref="InputMoverComponent"/>, we instead track it
/// as a separate component which is much faster to query all at once.
/// </remarks>
/// <seealso cref="InputMoverComponent"/>
/// <seealso cref="SharedMoverController.UpdateMoverStatus"/>
[RegisterComponent, Access(typeof(SharedMoverController))]
public sealed partial class ActiveInputMoverComponent : Component
{
    /// <summary>
    /// Cached version of <see cref="MovementRelayTargetComponent.Source"/>.
    /// </summary>
    /// <remarks>
    /// This <i>must not</i> form a loop of EntityUids.
    /// </remarks>
    [DataField, ViewVariables]
    public EntityUid? RelayedFrom;
};
