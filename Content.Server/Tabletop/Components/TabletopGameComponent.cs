using Content.Shared.Whitelist;

namespace Content.Server.Tabletop.Components
{
    /// <summary>
    /// A component that makes an object playable as a tabletop game.
    /// </summary>
    [RegisterComponent, Access(typeof(TabletopSystem))]
    public sealed class TabletopGameComponent : Component
    {
        [DataField("boardName")]
        public string BoardName { get; } = "tabletop-default-board-name";

        [DataField("pieceMaxSize")]
        public int PieceMaxSize { get; } = 10;

        [DataField("setup", required: true)]
        public TabletopSetup Setup { get; } = new TabletopChessSetup();

        [DataField("size")]
        public Vector2i Size { get; } = (300, 300);

        [DataField("cameraZoom")]
        public Vector2 CameraZoom { get; } = Vector2.One;

        [DataField("dumpPiecesOnPickup")]
        public bool DumpPiecesOnPickup { get; } = true;

        [DataField("blacklist")]
        public EntityWhitelist? Blacklist;

        [ViewVariables]
        public TabletopSession? Session { get; set; } = null;
    }
}
