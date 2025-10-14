using Content.Shared.ParcelWrap.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ParcelWrap.Components;

/// <summary>
/// This component marks its owner as being a parcel created by wrapping another item up. It can be unwrapped,
/// destroying this entity and releasing <see cref="Contents"/>.
/// </summary>
/// <seealso cref="ParcelWrapComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ParcelWrappingSystem))]
public sealed partial class WrappedParcelComponent : Component
{
    /// <summary>
    /// The contents of this parcel.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ContainerSlot Contents = default!;

    /// <summary>
    /// Specifies the entity to spawn when this parcel is unwrapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? UnwrapTrash;

    /// <summary>
    /// How long it takes to unwrap this parcel.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan UnwrapDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sound played when unwrapping this parcel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? UnwrapSound;

    /// <summary>
    /// The ID of <see cref="Contents"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string ContainerId = "contents";

    /// <summary>
    /// If a player trapped inside this parcel can escape from it by unwrapping it.
    /// This is set by the <see cref="ParcelWrapComponent" /> used to create the parcel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanSelfUnwrap = true;
}
