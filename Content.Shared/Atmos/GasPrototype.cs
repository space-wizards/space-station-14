using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Atmos
{
    /// <summary>
    /// Convenience struct for representing a gas ID.
    /// </summary>
    /// <remarks>Should be elided at runtime.</remarks>
    [Serializable, NetSerializable]
    public struct Gas : IEquatable<Gas>
    {
        private string _id;
        public string Id => _id;

        public Gas(string id)
        {
            _id = id;
        }

        public bool Equals(Gas other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is Gas && Equals((Gas) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Gas a, Gas b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Gas a, Gas b)
        {
            return !(a == b);
        }
    }

    [Prototype("gas")]
    public class GasPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private string _overlayPath;

        // TODO: Control gas amount necessary for overlay to appear
        // TODO: Add interfaces for gas behaviours e.g. breathing, burning

        public string ID => _id;

        /// <summary>
        /// Path to the tile overlay used when this gas appears visible.
        /// </summary>
        public string OverlayPath => _overlayPath;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _overlayPath, "overlayPath", string.Empty);
        }
    }
}
