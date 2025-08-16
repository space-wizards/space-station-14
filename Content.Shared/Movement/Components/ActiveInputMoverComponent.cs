using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Marker component for entities that are being processed by MoverController.
/// </summary>
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
