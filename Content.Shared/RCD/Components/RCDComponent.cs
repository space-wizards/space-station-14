using Content.Shared.Maps;
using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.RCD.Components;

public enum RcdMode : byte
{
    Floors,
    Walls,
    Airlock,
    Deconstruct
}

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
    /// Time taken to do an action like placing a wall
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Delay = 2f;

    [DataField("swapModeSound")]
    public SoundSpecifier SwapModeSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

    [DataField("successSound")]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// What mode are we on? Can be floors, walls, airlock, deconstruct.
    /// </summary>
    [DataField("mode"), AutoNetworkedField]
    public RcdMode Mode = RcdMode.Floors;

    /// <summary>
    /// ID of the floor to create when using the floor mode.
    /// </summary>
    [DataField("floor", customTypeSerializer: typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string Floor = "FloorSteel";
}
