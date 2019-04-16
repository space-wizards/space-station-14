using System.Collections.Generic;
using Content.Server.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    public class MaterialComponent : Component
    {
        public const string SerializationCache = "mat";
        public override string Name => "Material";

        Dictionary<object, Material> MaterialTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            if (!serializer.Reading)
            {
                return;
            }

            if (serializer.TryGetCacheData(SerializationCache, out Dictionary<object, Material> cached))
            {
                MaterialTypes = cached.ShallowClone();
                return;
            }

            MaterialTypes = new Dictionary<object, Material>();

            if (serializer.TryReadDataField("materials", out List<MaterialDataEntry> list))
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                int index = 0;
                foreach (var entry in list)
                {
                    var proto = protoMan.Index<MaterialPrototype>(entry.Value);
                    MaterialTypes[entry.Key] = proto.Material;
                    index++;
                }
            }

            serializer.SetCacheData(SerializationCache, MaterialTypes.ShallowClone());
        }

        class MaterialDataEntry : IExposeData
        {
            public object Key;
            public string Value;

            public void ExposeData(ObjectSerializer serializer)
            {
                if (!serializer.Reading)
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
