using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    [RegisterComponent]
    public class MaterialComponent : Component
    {
        public const string SerializationCache = "mat";
        public override string Name => "Material";

        public Dictionary<object, Material> MaterialTypes => _materialTypes;
        private Dictionary<object, Material> _materialTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            if (serializer.Writing)
            {
                return;
            }

            if (serializer.TryGetCacheData(SerializationCache, out Dictionary<object, Material> cached))
            {
                _materialTypes = cached.ShallowClone();
                return;
            }

            _materialTypes = new Dictionary<object, Material>();

            if (serializer.TryReadDataField("materials", out List<MaterialDataEntry> list))
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                foreach (var entry in list)
                {
                    var proto = protoMan.Index<MaterialPrototype>(entry.Value);
                    _materialTypes[entry.Key] = proto.Material;
                }
            }

            serializer.SetCacheData(SerializationCache, _materialTypes.ShallowClone());
        }

        class MaterialDataEntry : IExposeData
        {
            public object Key;
            public string Value;

            void IExposeData.ExposeData(ObjectSerializer serializer)
            {
                if (serializer.Writing)
                {
                    return;
                }

                var refl = IoCManager.Resolve<IReflectionManager>();
                Value = serializer.ReadDataField<string>("mat");
                var key = serializer.ReadDataField<string>("key");
                if (refl.TryParseEnumReference(key, out var @enum))
                {
                    Key = @enum;
                    return;
                }
                Key = key;
            }
        }
    }

    public enum MaterialKeys
    {
        Stack,
    }
}
