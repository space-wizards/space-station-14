using System;
using Content.Shared.Audio;
using Robust.Shared;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Sound
{
    [DataDefinition]
    public abstract class SoundSpecifier
    {
        public abstract string GetSound();
    }

    [DataDefinition]
    public class SoundEmptySpecifier : SoundSpecifier
    {
        public override string GetSound()
        {
            return string.Empty;
        }
    }

    [DataDefinition]
    public class SoundPathSpecifier : SoundSpecifier
    {
        public const string Node = "path";

        [DataField(Node, customTypeSerializer:typeof(ResourcePathSerializer), required:true)]
        public ResourcePath Path { get; } = default!;

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

        public override string GetSound()
        {
            return Path.ToString();
        }
    }

    [DataDefinition]
    public class SoundCollectionSpecifier : SoundSpecifier
    {
        public const string Node = "collection";

        [DataField(Node, customTypeSerializer:typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required:true)]
        public string Collection { get; } = default!;

        public SoundCollectionSpecifier()
        {
        }

        public SoundCollectionSpecifier(string collection)
        {
            Collection = collection;
        }

        public override string GetSound()
        {
            return AudioHelpers.GetRandomFileFromSoundCollection(Collection);
        }
    }
}
