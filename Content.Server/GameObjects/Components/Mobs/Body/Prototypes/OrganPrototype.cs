using System;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Reflection;

namespace Content.Server.GameObjects.Components.Mobs.Body.Organs
{
    /// <summary>
    /// This class is a layer between YAML and actual <see cref="Organ"/> class. It's being created by YAML serialization and creates the object itself on body initialization
    /// </summary>
    [Prototype("organ")]
    public class OrganPrototype : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency]
        protected IReflectionManager reflectionManager;
#pragma warning restore

        string IIndexedPrototype.ID => Id;

        public string Name { get; private set; }
        public string Id { get; private set; }
        public string PrototypeEntity { get; private set; }//entity that spawns on place of the organ, useful for gibs and surgery

        public int MaxHealth { get; private set; }

        string ClassName;
        YamlMappingNode _mapping;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            Name = serializer.ReadDataField("name", "");
            Id = serializer.ReadDataField("id", "");
            MaxHealth = serializer.ReadDataField("health", 0);
            PrototypeEntity = serializer.ReadDataField("prototype", "");
            serializer.DataField(ref ClassName, "parent", "");
            _mapping = mapping;
        }

        public Organ Create()
        {
            if (string.IsNullOrEmpty(ClassName))
            {
                throw new InvalidOperationException("Organ doesn't have an instance to create.");
            }
            Type newtype = reflectionManager.GetType("Content.Server.GameObjects.Components.Mobs.Body.Organs." + ClassName);
            var organ = (Organ)Activator.CreateInstance(newtype);
            organ.DataFromPrototype(this);
            organ.ExposeData(_mapping);
            return organ;
        }
    }
}
