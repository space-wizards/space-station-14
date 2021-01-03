using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Materials
{
    public class MaterialComponentDataClass
    {
        [CustomYamlField("materials")]
        public Dictionary<object, Material> MaterialTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            if (serializer.Writing)
            {
                return;
            }

            MaterialTypes = new Dictionary<object, Material>();

            if (serializer.TryReadDataField("materials", out List<MaterialComponent.MaterialDataEntry> list))
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                foreach (var entry in list)
                {
                    var proto = protoMan.Index<MaterialPrototype>(entry.Value);
                    MaterialTypes[entry.Key] = proto.Material;
                }
            }
        }
    }
}
