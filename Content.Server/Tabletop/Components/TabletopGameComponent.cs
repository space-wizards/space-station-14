using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

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

        [DataField("setup")]
        public TabletopSetup Setup { get; } = new();

        /// <summary>
        ///     A dictionary for pieces for <code>PieceGrids</code>.
        /// </summary>

        [DataField("pieces", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<EntityPrototype, char>))]
        public Dictionary<char, string> Pieces { get; } = new();

        [DataField("pieceGrids")]
        public List<PieceGrid> PieceGrids = new();

        /// <summary>
        ///     Pieces (or entities) that does not belong to a grid.
        ///     Tabletop item belongs to here too.
        /// </summary>
        [DataField("freePieces")]
        public List<FreePiece> FreePieces { get; } = new();

        [DataField("size")]
        public Vector2i Size { get; } = (300, 300);

        [DataField("cameraZoom")]
        public Vector2 CameraZoom { get; } = Vector2.One;

        [ViewVariables]
        public TabletopSession? Session { get; set; } = null;
    }

    public class PieceGrid
    {
        [IdDataField]
        public string ID = default!;

        [DataField("separation")]
        public Vector2 Separation = (1.0f, -1.0f);

        [DataField("startingPosition")]
        public Vector2 StartingPosition = (0.0f, 0.0f);

        [DataField("pieceString", required: true)]
        public string PieceString = "";
    }

    public class FreePiece
    {
        [DataField("piece", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string PiecePrototype = "";

        [DataField("position")]
        public Vector2 Position = (0.0f, 0.0f);
   }
}
