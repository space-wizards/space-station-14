using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Serialization;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs.Body.Organs
{
    /// <summary>
    /// This class is a layer between YAML and actual <see cref="Organ"/> class. It's being created by YAML serialization and creates the object itself on body initialization
    /// </summary>
    [Prototype("organ")]
    public class OrganPrototype : IPrototype, IIndexedPrototype
    {
        string Name;
        string Id;
        string IIndexedPrototype.ID => Id;
        int MaxHealth;
        string PrototypeEnitity; //entity that spawns on place of the organ, useful for gibs and surgery
        string Parent;
        YamlMappingNode _mapping;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref Name, "name", "");
            serializer.DataField(ref Id, "id", "");
            serializer.DataField(ref MaxHealth, "health", 0);
            serializer.DataField(ref PrototypeEnitity, "prototype", "");
            serializer.DataField(ref Parent, "parent", "");
            _mapping = mapping;
        }

        public Organ Create()
        {
            if (string.IsNullOrEmpty(Parent))
            {
                return null;
            }
            Type newtype = AppDomain.CurrentDomain.GetAssemblyByName("Content.Server")
                .GetType("Content.Server.GameObjects.Components.Mobs.Body.Organs." + Parent);
            var organ = (Organ)Activator.CreateInstance(newtype);
            organ.Name = Name;
            organ.Id = Id;
            organ.MaxHealth = MaxHealth;
            organ.PrototypeEnitity = PrototypeEnitity;
            organ.ExposeData(_mapping);
            return organ;
        }
    }
}
