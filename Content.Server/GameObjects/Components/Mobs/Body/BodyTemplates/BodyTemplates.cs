using System;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///    Core of the mobcode. It glues all the shitcode with limbs, organs 
    ///    and body functions together with DAMAGE, making frankensteins that we call Mobs
    /// </summary>
    public class BodyTemplate
    {
        public string Name;
        public List<Limb> bodyMap;
        public IEntity Owner;

        public Blood Blood; //blood should wait for reagents to get truly implemented

        private Random _randomLimb;

        public virtual void ExposeData(ObjectSerializer obj)
        {
            obj.DataField(ref bodyMap, "limbs", null);
            obj.DataField(ref Blood, "blood", null);
        }

        public virtual void Initialize(IEntity owner)
        {
            Owner = owner;
            _randomLimb = new Random(owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
            foreach (var limb in bodyMap)
            {

            }
        }

        public void Life(int lifeTick) //this is main Life() proc!
        {
            foreach(var limb in bodyMap)
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
            _randomLimb.Pick(bodyMap).HandleDamage(damage);
        }

        public void Gib()
        {
            foreach (var limb in bodyMap)
            {
                limb.HandleGib();
            }
            bodyMap = null;
        }

        public List<LimbRender> RenderDoll()
        {
            var list = new List<LimbRender>();
            foreach (var limb in bodyMap)
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
