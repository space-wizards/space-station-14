using System.Numerics;
using Vector2 = System.Numerics.Vector2;

namespace Content.Server.Tabletop.Components
{
    /// <summary>
    /// A component that makes an object playable as a tabletop game.
    /// </summary>
    [RegisterComponent, Access(typeof(TabletopSystem))]
    public sealed partial class TabletopGameComponent : Component
    {
        /// <summary>
        /// The localized name of the board. Shown in the UI.
        /// </summary>
        [DataField("boardName")]
        public string BoardName { get; private set; } = "tabletop-default-board-name";

        /// <summary>
        /// The type of method used to set up a tabletop.
        /// </summary>
        [DataField("setup", required: true)]
        public TabletopSetup Setup { get; private set; } = new TabletopChessSetup();

        /// <summary>
        /// The size of the viewport being opened. Must match the board dimensions otherwise you'll get the space parallax (unless that's what you want).
        /// </summary>
        [DataField("size")]
        public Vector2i Size { get; private set; } = (300, 300);

        /// <summary>
        /// The zoom of the viewport camera.
        /// </summary>
        [DataField("cameraZoom")]
        public Vector2 CameraZoom { get; private set; } = Vector2.One;

        /// <summary>
        /// The specific session of this tabletop.
        /// </summary>
        [ViewVariables]
        public TabletopSession? Session { get; set; } = null;
    }
}
