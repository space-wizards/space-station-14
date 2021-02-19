using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

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

        public Dictionary<object, MaterialPrototype> MaterialTypes => _materialTypes;
        [DataClassTarget("materialsTarget")]
        private Dictionary<object, MaterialPrototype> _materialTypes;

        public class MaterialDataEntry : ISerializationHooks
        {
            public object Key;

            [DataField("key")]
            public string StringKey;

            [DataField("mat")]
            public string Value;

            public void AfterDeserialization()
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
