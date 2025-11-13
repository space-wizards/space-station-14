using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ParcelWrap.Components;

/// <summary>
/// Added to an entity to override the datafields in <see cref="ParcelWrapComponent"/> when it is being wrapped.
/// Use this for special, non-item parcel types, for example Urist-shaped parcels.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParcelWrapOverrideComponent : Component
{
    /// <summary>
    /// The <see cref="EntityPrototype"/> of the parcel created by wrapping this entity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? ParcelPrototype;

    /// <summary>
    /// How long it takes to use this to wrap something.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan? WrapDelay;
}
