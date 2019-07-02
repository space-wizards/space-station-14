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
    ///    Core of the mobcode. It glues all the shitcode with limbs, organs 
    ///    and body functions together with DAMAGE, making frankensteins that we call Mobs
    /// </summary>

    public class BodyTemplate
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
        private List<Limb> Limbs;



        /// <summary>
        /// Entity which owns MobComponent
        /// </summary>
        public IEntity Owner;

        /// <summary>
        /// List of YAML strings which inits all the limbs
        /// </summary>
        private List<string> _limbProts;

        Random _randomLimb
        {
            get 
            {
                return new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
            }
        }

        public void DataFromPrototype(BodyPrototype prototype)
        {
            Name = prototype.Name;
            Id = prototype.Id;
            _limbProts = prototype.LimbPrototypes;
        }

        public void Initialize(IEntity owner, IPrototypeManager prototypeManager)
        {
            Owner = owner;
            PrototypeManager = prototypeManager;
            TimeSinceUpdate = 0;
            Limbs = new List<Limb>();
            foreach (var limbProtKey in _limbProts)
            {
                if (PrototypeManager.TryIndex<LimbPrototype>(limbProtKey, out var limbProt))
                {
                    var limb = limbProt.Create();
                    limb.Initialize(Owner, this);
                    Limbs.Add(limb);
                }
            }
            FillTheLists();
        }

        private void FillTheLists()
        {
            BodyMap = new List<Limb>();
            AllBodyParts = new List<BodyPart>();
            foreach (var limb in Limbs)
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
                    if (PrototypeManager.TryIndex<OrganPrototype>(organProtKey, out var organProt))
                    {
                        var organ = organProt.Create();
                        organ.Initialize(Owner, this);
                        limb.Organs.Add(organ);
                        AllBodyParts.Add(organ);
                    }
                }

            }
            Limbs = null;
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

        public void RemovePart(BodyPart part)
        {
            if (part is Limb)
            {
                BodyMap.Remove((Limb)part);
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
