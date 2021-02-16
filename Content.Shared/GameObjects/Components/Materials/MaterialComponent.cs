using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    [RegisterComponent]
    [DataClass(typeof(MaterialComponentDataClass))]
    public class MaterialComponent : Component
    {
        public const string SerializationCache = "mat";
        public override string Name => "Material";

        public Dictionary<object, Material> MaterialTypes => _materialTypes;
        [DataClassTarget("materials")]
        private Dictionary<object, Material> _materialTypes;

        public class MaterialDataEntry : IExposeData
        {
            public object Key;
            public string Value;

            public void ExposeData(ObjectSerializer serializer)
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
