using Content.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Sound
{
    [DataDefinition]
    public abstract class SoundSpecifier
    {
        public abstract string GetSound();

        public abstract bool TryGetSound([NotNullWhen(true)] out string? sound);
    }

    [DataDefinition]
    public sealed class SoundPathSpecifier : SoundSpecifier
    {
        public const string Node = "path";

        [DataField(Node, customTypeSerializer: typeof(ResourcePathSerializer), required: true)]
        public ResourcePath? Path { get; }

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
            return Path == null ? string.Empty : Path.ToString();
        }
    
        public override bool TryGetSound([NotNullWhen(true)] out string? sound)
        {
            sound = GetSound();
            return !string.IsNullOrWhiteSpace(sound);
        }
    }

    [DataDefinition]
    public sealed class SoundCollectionSpecifier : SoundSpecifier
    {
        public const string Node = "collection";

        [DataField(Node, customTypeSerializer: typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required: true)]
        public string? Collection { get; }

        public SoundCollectionSpecifier()
        {
        }

        public SoundCollectionSpecifier(string collection)
        {
            Collection = collection;
        }

        public override string GetSound()
        {
            return Collection == null ? string.Empty : AudioHelpers.GetRandomFileFromSoundCollection(Collection);
        }

        public override bool TryGetSound([NotNullWhen(true)] out string? sound)
        {
            sound = GetSound();
            return !string.IsNullOrWhiteSpace(sound);
        }
    }
}
