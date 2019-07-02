using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.Interfaces.Reflection;
using Content.Server.GameObjects.Components.Mobs.Body.Organs;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Limb is not just like <see cref="Organ"/>, it has BONES, and holds organs (and child limbs), 
    ///     it receive damage first, then through resistances and such it transfers the damage to organs,
    ///     also the limb is visible, and it can be targeted
    /// </summary>
    [Prototype("limb")]
    public class LimbPrototype : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency]
        protected IReflectionManager reflectionManager;
#pragma warning restore

        string IIndexedPrototype.ID => Id;

        public string Name { get; private set; }
        public string Id { get; private set; }
        private string ClassName = "Limb";
        public string Parent { get; private set; }
        public string TexturePath { get; private set; }
        public string PrototypeEntity { get; private set; }

        public List<string> OrganPrototypes { get; private set; }

        public bool ChildOrganDamage { get; private set; }
        public bool DirectOrganDamage { get; private set; }

        public int MaxHealth { get; private set; }

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            var obj = YamlObjectSerializer.NewReader(mapping);
            Id = obj.ReadDataField("id", "");
            Name = obj.ReadDataField("name", "");
            MaxHealth = obj.ReadDataField("health", 0);
            TexturePath = obj.ReadDataField("dollIcon", "");
            PrototypeEntity = obj.ReadDataField("prototype", "");
            Parent = obj.ReadDataField("parent", "");
            ChildOrganDamage = obj.ReadDataField("childOrganDamage", false);
            DirectOrganDamage = obj.ReadDataField("directOrganDamage", false);

            OrganPrototypes = new List<string>();
            if (mapping.TryGetNode<YamlSequenceNode>("organs", out var organNodes))
            {
                foreach (var prot in organNodes.Cast<YamlMappingNode>())
                {
                    OrganPrototypes.Add(prot.GetNode("map").AsString());
                }
            }
        }

        public Limb Create()
        {
            if (string.IsNullOrEmpty(ClassName))
            {
                throw new InvalidOperationException("Limb doesn't have an instance to create.");
            }
            Type newtype = reflectionManager.GetType("Content.Server.GameObjects.Components.Mobs.Body." + ClassName);
            var limb = (Limb)Activator.CreateInstance(newtype);
            limb.DataFromPrototype(this);
            return limb;
        }
    }
}
