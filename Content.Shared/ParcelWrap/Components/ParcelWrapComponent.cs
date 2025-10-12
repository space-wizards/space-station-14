using Content.Shared.Item;
using Content.Shared.ParcelWrap.Systems;
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
    public EntProtoId<WrappedParcelComponent> ParcelPrototype;

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
