#nullable enable
using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using static Content.Shared.GameObjects.Components.Materials.MaterialComponent;

namespace Content.Shared.GameObjects.Components.Materials
{
    public partial class MaterialComponentDataClass : ISerializationHooks
    {
        [DataField("materials")]
        private List<MaterialDataEntry>? _materials;

        [DataClassTarget("materialsTarget")]
        public Dictionary<object, MaterialPrototype> MaterialTypes = new Dictionary<object, MaterialPrototype>();

        public void AfterDeserialization()
        {
            if (_materials != null)
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();

                foreach (var entry in _materials)
                {
                    var proto = protoMan.Index<MaterialPrototype>(entry.Value);
                    MaterialTypes[entry.Key] = proto;
                }
            }
        }
    }
}
