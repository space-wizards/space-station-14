using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Sound
{
    [ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
    public abstract class SoundSpecifier
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("params")]
        public AudioParams Params = AudioParams.Default;

        // TODO: remove most uses of this function, and just make the audio-system take in a SoundSpecifier
        public abstract string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null);
    }

    [Serializable, NetSerializable]
    public sealed class SoundPathSpecifier : SoundSpecifier
    {
        public const string Node = "path";

        [DataField(Node, customTypeSerializer: typeof(ResourcePathSerializer), required: true)]
        public ResourcePath? Path { get; }

        [UsedImplicitly]
        public SoundPathSpecifier()
        {
        }

        public SoundPathSpecifier(string path)
        {
            Path = new ResourcePath(path);
        }

        public SoundPathSpecifier(ResourcePath path)
        {
            Path = path;
        }

        public override string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null)
        {
            return Path == null ? string.Empty : Path.ToString();
        }
    }

    [Serializable, NetSerializable]
    public sealed class SoundCollectionSpecifier : SoundSpecifier
    {
        public const string Node = "collection";

        [DataField(Node, customTypeSerializer: typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required: true)]
        public string? Collection { get; }

        [UsedImplicitly]
        public SoundCollectionSpecifier()
        {
        }

        public SoundCollectionSpecifier(string collection)
        {
            Collection = collection;
        }

        public override string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null)
        {
            return Collection == null ? string.Empty : AudioHelpers.GetRandomFileFromSoundCollection(Collection, rand, proto);
        }
    }
}
