using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.Interfaces.Reflection;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///    Core of the mobcode. It glues all the shitcode with limbs, organs 
    ///    and body functions together with DAMAGE, making frankensteins that we call Mobs
    /// </summary>

    [Prototype("bodyTemplate")]
    public class BodyPrototype : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency]
        protected IReflectionManager reflectionManager;
#pragma warning restore

        string IIndexedPrototype.ID => Id;

        public string Name { get; private set; }
        public string Id { get; private set; }
        string ClassName;

        public List<string> LimbPrototypes { get; private set; }

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            var obj = YamlObjectSerializer.NewReader(mapping);
            Id = obj.ReadDataField("id", "");
            Name = obj.ReadDataField("name", "");
            obj.DataField(ref ClassName, "className", "");
            LimbPrototypes = new List<string>();
            foreach (var limbMap in mapping.GetNode<YamlSequenceNode>("limbs").Cast<YamlMappingNode>())
            {
                var limbProt = limbMap.GetNode("map").AsString();
                LimbPrototypes.Add(limbProt);
            }
        }

        public BodyInstance Create()
        {
            if (string.IsNullOrEmpty(ClassName))
            {
                throw new InvalidOperationException("Organ doesn't have an instance to create.");
            }
            Type newtype = reflectionManager.GetType("Content.Server.GameObjects.Components.Mobs.Body." + ClassName);
            var body = (BodyInstance)Activator.CreateInstance(newtype);
            body.DataFromPrototype(this);
            return body;
        }
    }
}
