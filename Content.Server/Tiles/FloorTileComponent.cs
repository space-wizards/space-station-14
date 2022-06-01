using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Tiles
{
    [RegisterComponent]
    public sealed class FloorTileComponent : Component
    {

        [DataField("outputs", customTypeSerializer: typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
        public List<string>? OutputTiles;

        [DataField("placeTileSound")]
        public SoundSpecifier PlaceTileSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");
    }
}
