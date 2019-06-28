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
    [Prototype("organ")]
    public class OrganPrototype : IPrototype, IIndexedPrototype
    {
        public string Name;
        public string Id;
        string IIndexedPrototype.ID => Id;
        public int MaxHealth;
        public int CurrentHealth;
        public float BloodChange = 0.005f; //TODO: Organs should consume reagents (nutriments) from blood, not blood directly
        public OrganState State = OrganState.Healthy;
        public List<OrganStatus> Statuses;
        public IEntity Owner;
        public string PrototypeEnitity; //entity that spawns on place of the organ, useful for gibs and surgery
        public string GibletEntity;
        public string Parent;
        public BodyTemplate Body;
        ObjectSerializer serializer;

        public void LoadFrom(YamlMappingNode mapping)
        {
            serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref Name, "name", "");
            serializer.DataField(ref Id, "id", "");
            serializer.DataField(ref MaxHealth, "health", 0);
            serializer.DataField(ref PrototypeEnitity, "prototype", "");
            serializer.DataField(ref Parent, "parent", "");
        }

        public Organ Create()
        {
            if (string.IsNullOrEmpty(Parent))
            {
                return null;
            }
            Type newtype = AppDomain.CurrentDomain.GetAssemblyByName("Content.Server")
                .GetType("Content.Server.GameObjects.Components.Mobs.Body.Organ" + Parent);
            var organ = (Organ)Activator.CreateInstance(newtype);
            organ.Name = Name;
            organ.Id = Id;
            organ.MaxHealth = MaxHealth;
            organ.PrototypeEnitity = PrototypeEnitity;
            organ.ExposeData(serializer);
            return organ;
        }
    }
}
