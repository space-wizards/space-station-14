using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Server.GameObjects.Components.Mobs.Body.Organs;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///    Core of the mobcode. Damage to bodyparts is handled here, as well as calling bodypart functions on specific gameticks.
    /// </summary>

    public class BodyInstance
    {
#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager PrototypeManager;
#pragma warning restore

        public string Name { get; private set; }
        public string Id { get; private set; }
        public float TimeSinceUpdate { get; private set; }

        /// <summary>
        /// All body parts - limbs and organs alike. From this list Life() is called.
        /// </summary>
        protected List<BodyPart> AllBodyParts;
        /// <summary>
        /// The list of all mob's limbs. From this list damage is spreading. 
        /// </summary>
        protected List<Limb> BodyMap;
        /// <summary>
        /// helper list to assign children to BodyMap
        /// </summary>
        private List<Limb> _helperLimbs;



        /// <summary>
        /// Entity which owns MobComponent
        /// </summary>
        public IEntity Owner;

        /// <summary>
        /// List of YAML strings which inits all the limbs
        /// </summary>
        private BodyPrototype _prototype;

        Random _randomLimb;

        public void DataFromPrototype(BodyPrototype prototype)
        {
            _prototype = prototype;
            Name = _prototype.Name;
            Id = _prototype.Id;
        }

        public void Initialize(IEntity owner, IPrototypeManager prototypeManager)
        {
            Owner = owner;
            PrototypeManager = prototypeManager;
            TimeSinceUpdate = 0;
            _randomLimb = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
            _helperLimbs = new List<Limb>();
            foreach (var limbProtKey in _prototype.LimbPrototypes)
            {
                var limbProt = PrototypeManager.Index<LimbPrototype>(limbProtKey);
                var limb = limbProt.Create();
                limb.Initialize(Owner, this);
                _helperLimbs.Add(limb);
            }
            BodyMap = new List<Limb>();
            AllBodyParts = new List<BodyPart>();
            foreach (var limb in _helperLimbs)
            {
                limb.Children = new List<Limb>();
                var children = FindChildren(limb.Id);
                if (children.Count > 0)
                {
                    limb.Children.AddRange(children);
                }
                BodyMap.Add(limb);
                AllBodyParts.Add(limb);

                foreach (var organProtKey in limb.OrganPrototypes)
                {
                    var organProt = PrototypeManager.Index<OrganPrototype>(organProtKey);
                    var organ = organProt.Create();
                    organ.Initialize(Owner, this);
                    limb.Organs.Add(organ);
                    AllBodyParts.Add(organ);
                }
            }
            _helperLimbs = null;
        }

        private List<Limb> FindChildren(string parentTag)
        {
            var list = new List<Limb>();
            foreach (var limb in _helperLimbs)
            {
                if (!string.IsNullOrEmpty(limb.Parent) && limb.Parent == parentTag)
                {
                    list.Add(limb);
                }
            }
            return list;
        }

        public void RemovePart(BodyPart part)
        {
            if (part is Limb limb)
            {
                var children = FindChildren(limb.Id);
                if (children.Count > 0)
                {
                    foreach (var child in children)
                    {
                        RemovePart(child);
                    }
                }
                BodyMap.Remove(limb);
            }
            AllBodyParts.Remove(part);
        }

        public virtual void Life(float frameTime) //this is main Life() proc!
        {
            TimeSinceUpdate += frameTime;
            foreach(var bodyPart in AllBodyParts)
            {
                bodyPart.Life(frameTime);
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
        }

        public List<LimbRender> RenderDoll()
        {
            var list = new List<LimbRender>();
            if (BodyMap != null)
            {
                foreach (var limb in BodyMap)
                {
                    if (!string.IsNullOrEmpty(limb.TexturePath))
                    {
                        list.Add(limb.Render());
                    }
                }
            }
            return list;
        }
    }
}
