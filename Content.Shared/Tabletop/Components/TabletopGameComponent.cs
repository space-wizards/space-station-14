using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// A component that makes an object playable as a tabletop game.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTabletopSystem), typeof(TabletopSetup))]
public sealed partial class TabletopGameComponent : Component
{
    /// <summary>
    /// The localized name of the board. Shown in the UI.
    /// </summary>
    [DataField]
    public LocId BoardName = "tabletop-default-board-name";

    /// <summary>
    /// The type of method used to set up a tabletop.
    /// </summary>
    [DataField(required: true)]
    public TabletopSetup Setup = new TabletopChessSetup();

    /// <summary>
    /// The size of the viewport being opened. Must match the board dimensions otherwise you'll get the space parallax (unless that's what you want).
    /// </summary>
    [DataField]
    public Vector2i Size;

    /// <summary>
    /// The offset, from the center of the board, that hologram pieces should be spawned in at.
    /// </summary>
    [DataField]
    public Vector2 SpawnOffset;

    /// <summary>
    /// The zoom of the viewport camera.
    /// </summary>
    [DataField]
    public Vector2 CameraZoom { get; private set; } = Vector2.One;

    /// <summary>
    /// The board entity for this game. Also functions as the camera.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EntityUid? Board;
}

/// <summary>
/// A UI key enum for board games.
/// </summary>
[Serializable, NetSerializable]
public enum TabletopGameUiKey
{
    Key
}
