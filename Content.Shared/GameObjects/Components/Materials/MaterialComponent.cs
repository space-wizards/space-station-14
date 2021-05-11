using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.GameObjects.Components.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    [RegisterComponent]
    public class MaterialComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public const string SerializationCache = "mat";

        public override string Name => "Material";

        [DataField("materials")] private List<MaterialDataEntry> _materials = new();

        public IEnumerable<KeyValuePair<object, MaterialPrototype>> MaterialTypes
        {
            get
            {
                foreach (var entry in _materials)
                {
                    var prototype = _prototypeManager.Index<MaterialPrototype>(entry.Value);

                    yield return new KeyValuePair<object, MaterialPrototype>(entry.Key, prototype);
                }
            }
        }

        [DataDefinition]
        public class MaterialDataEntry : ISerializationHooks
        {
            public object Key = default!;

            [DataField("key", required: true)]
            public string StringKey = default!;

            [DataField("mat", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<MaterialPrototype>))]
            public string Value = default!;

            void ISerializationHooks.AfterDeserialization()
            {
                var refl = IoCManager.Resolve<IReflectionManager>();

                if (refl.TryParseEnumReference(StringKey, out var @enum))
                {
                    Key = @enum;
                    return;
                }

                Key = StringKey;
            }
        }
    }

    public enum MaterialKeys
    {
        Stack,
    }
}
