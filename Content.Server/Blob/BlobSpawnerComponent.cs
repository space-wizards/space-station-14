using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Blob
{
    [RegisterComponent]
    public sealed class BlobSpawnerComponent : Component
    {
        [DataField("spawnSound")]
        public SoundSpecifier SpawnSoundPath = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("corePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CoreBlobPrototype = "CoreBlobTile";

        [ViewVariables(VVAccess.ReadWrite),
         DataField("ghostPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ObserverBlobPrototype = "MobObserverBlob";
    }
}
