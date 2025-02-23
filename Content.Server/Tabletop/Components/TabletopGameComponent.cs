using System.Numerics;
using Content.Server.Tabletop.Game;

namespace Content.Server.Tabletop.Components;

/// <summary>
/// A component that makes an object playable as a tabletop game.
/// </summary>
[RegisterComponent, Access(typeof(TabletopSystem))]
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
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public TabletopSetup Setup = new TabletopEmptySetup();

    /// <summary>
    /// The size of the viewport being opened. Must match the board dimensions otherwise you'll get the space parallax
    /// (unless that's what you want).
    /// </summary>
    [DataField]
    public Vector2i Size = (300, 300);

    /// <summary>
    /// The zoom of the viewport camera.
    /// </summary>
    [DataField]
    public Vector2 CameraZoom = Vector2.One;

    /// <summary>
    /// The specific session of this tabletop.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TabletopSession? Session = null;
}
