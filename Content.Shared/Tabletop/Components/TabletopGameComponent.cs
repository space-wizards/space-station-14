using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// A component that makes an object playable as a tabletop game.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTabletopSystem), typeof(TabletopSetup))]
public sealed partial class TabletopGameComponent : Component
{
    /// <summary>
    /// The localized name of the board. Shown in the UI.
    /// </summary>
    [DataField]
    public LocId BoardName { get; private set; } = "tabletop-default-board-name";

    /// <summary>
    /// The type of method used to set up a tabletop.
    /// </summary>
    [DataField(required: true)]
    public TabletopSetup Setup { get; private set; } = new TabletopChessSetup();

    /// <summary>
    /// The size of the viewport being opened. Must match the board dimensions otherwise you'll get the space parallax (unless that's what you want).
    /// </summary>
    [DataField]
    public Vector2i Size { get; private set; } = (300, 300);

    /// <summary>
    /// The zoom of the viewport camera.
    /// </summary>
    [DataField]
    public Vector2 CameraZoom { get; private set; } = Vector2.One;

    /// <summary>
    /// The position of the session. If the map is invalid, this game does not have a table set up.
    /// Useful for both server and client.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MapCoordinates? Position;

    /// <summary>
    /// Convenience field, returns whether or not the game has an active session.
    /// </summary>
    public bool HasSession => Position != null;

    /// <summary>
    /// The set of players currently playing this tabletop game.
    /// </summary>
    [DataField(serverOnly: true), ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ICommonSession, TabletopSessionPlayerData> Players = [];

    /// <summary>
    /// All non-camera entities bound to this session. If you create an entity for this session, you have to add it here.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public HashSet<EntityUid> Entities = [];
}
