using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.RCD.Components;

/// <summary>
/// Main component for the RCD
/// Optionally uses LimitedChargesComponent.
/// Charges can be refilled with RCD ammo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RCDSystem))]
public sealed partial class RCDComponent : Component
{
    /// <summary>
    /// List of RCD prototypes that the device comes loaded with
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RCDPrototype>> AvailablePrototypes = new();

    /// <summary>
    /// Sound that plays when a RCD operation successfully completes
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// The ProtoId of the currently selected RCD prototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RCDPrototype> ProtoId = default!;

    /// <summary>
    /// A cached copy of currently selected RCD prototype
    /// </summary>
    /// <remarks>
    /// If the ProtoId is changed, make sure to update the CachedPrototype as well
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public RCDPrototype CachedPrototype = default!;

    /// <summary>
    /// The direction constructed entities will face upon spawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction ConstructionDirection = Direction.South;
}
