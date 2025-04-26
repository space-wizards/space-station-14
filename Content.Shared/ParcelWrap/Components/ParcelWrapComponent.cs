using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ParcelWrap.Components;

/// <summary>
/// This component gives its owning entity the ability to wrap items into parcels.
/// </summary>
/// <seealso cref="Components.WrappedParcelComponent"/>
[RegisterComponent, NetworkedComponent]
[Access] // Readonly, except for VV editing
public sealed partial class ParcelWrapComponent : Component
{
    /// <summary>
    /// The <see cref="EntityPrototype"/> of the parcel created by using this component.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ParcelPrototype;

    /// <summary>
    /// If true, parcels created by this will have the same <see cref="ItemSizePrototype">size</see> as the item they
    /// contain. If false, parcels created by this will always have the size specified by <see cref="FallbackItemSize"/>.
    /// </summary>
    [DataField]
    public bool WrappedItemsMaintainSize = true;

    /// <summary>
    /// The <see cref="ItemSizePrototype">size</see> of parcels created by this component's entity. This is used if
    /// <see cref="WrappedItemsMaintainSize"/> is false, or if the item being wrapped somehow doesn't have a size.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype> FallbackItemSize = "Ginormous";

    /// <summary>
    /// If true, parcels created by this will have the same shape as the item they contain. If false, parcels created by
    /// this will have the default shape for their size.
    /// </summary>
    [DataField]
    public bool WrappedItemsMaintainShape;

    /// <summary>
    /// How long it takes to use this to wrap something.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan WrapDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sound played when this is used to wrap something.
    /// </summary>
    [DataField]
    public SoundSpecifier? WrapSound;

    /// <summary>
    /// Defines the set of things which can be wrapped (unless it fails the <see cref="Blacklist"/>).
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Defines the set of things which cannot be wrapped (even if it passes the <see cref="Whitelist"/>).
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
