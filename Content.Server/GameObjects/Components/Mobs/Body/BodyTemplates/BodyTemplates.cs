using System;
using System.Linq;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using YamlDotNet.RepresentationModel;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///    Core of the mobcode. It glues all the shitcode with limbs, organs 
    ///    and body functions together with DAMAGE, making frankensteins that we call Mobs
    /// </summary>

    [Prototype("bodyTemplate")]
    public class BodyTemplate : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency]
        protected IPrototypeManager PrototypeManager;
#pragma warning restore

        public string Name;
        public string Id;
        public List<Limb> BodyMap;
        public List<Limb> Limbs;
        public IEntity Owner;

        private string _bloodProt;
        private List<string> _limbProts;
        public Blood Blood; //blood should wait for reagents to get truly implemented

        private Random _randomLimb;

        string IIndexedPrototype.ID => Id;

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            var obj = YamlObjectSerializer.NewReader(mapping);
            obj.DataField(ref Id, "id", "");
            obj.DataField(ref _bloodProt, "blood", "");
            _limbProts = new List<string>();
            foreach (var limbMap in mapping.GetNode<YamlSequenceNode>("limbs").Cast<YamlMappingNode>())
            {
                var limbProt = limbMap.GetNode("map").AsString();
                _limbProts.Add(limbProt);
            }
        }

        public void Initialize(IEntity owner)
        {
            Owner = owner;
            PrototypeManager = IoCManager.Resolve<IPrototypeManager>();
            _randomLimb = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
            Limbs = new List<Limb>();
            foreach (var limbProt in _limbProts)
            {
                if (PrototypeManager.TryIndex<Limb>(limbProt, out var limb))
                {
                    limb.Initialize(Owner, this);
                    Limbs.Add(limb);
                }
            }

            BodyMap = InitBodyMap();

            if (PrototypeManager.TryIndex<Blood>(_bloodProt, out var blood))
            {
                Blood = blood;
            }
        }

        private List<Limb> InitBodyMap()
        {
            var bodyMap = new List<Limb>();
            foreach (var limb in Limbs)
            {
                limb.Children = new List<Limb>();
                var children = FindChildren(limb.Id);
                if (children.Count > 0)
                {
                    limb.Children.AddRange(children);
                }
                bodyMap.Add(limb);

            }
            return bodyMap;
        }

        private List<Limb> FindChildren(string parentTag)
        {
            var list = new List<Limb>();
            foreach (var limb in Limbs)
            {
                if (!string.IsNullOrEmpty(limb.Parent) && limb.Parent == parentTag)
                {
                    list.Add(limb);
                }
            }
            return list;
        }

        public virtual void Life(int lifeTick) //this is main Life() proc!
        {
            foreach(var limb in BodyMap)
            {
                Blood = limb.CirculateBlood(Blood);
                foreach(var organ in limb.Organs)
                {
                    organ.Life(lifeTick);
                    organ.CirculateBlood(Blood);
                }
            }
        }

        public void HandleDamage(DamageType damageType, int damage)
        {
            //TODO: Targetting.
            //bodyMap[2].HandleDamage(damage, new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode())); //testing specific limb's (head) damage
            _randomLimb.Pick(BodyMap).HandleDamage(damage);
        }

        public void Gib()
        {
            foreach (var limb in BodyMap)
            {
                limb.HandleGib();
            }
            BodyMap = null;
        }

        public List<LimbRender> RenderDoll()
        {
            var list = new List<LimbRender>();
            foreach (var limb in BodyMap)
            {
                if (!string.IsNullOrEmpty(limb.RenderLimb))
                {
                    list.Add(limb.Render());
                }
            }
            return list;
        }
    }
}
