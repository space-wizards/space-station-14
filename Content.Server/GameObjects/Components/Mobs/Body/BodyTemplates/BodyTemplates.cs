using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Mobs;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///    Core of the mobcode. It glues all the shitcode with limbs, organs 
    ///    and body functions together with DAMAGE, making frankensteins that we call Mobs
    /// </summary>
    public class BodyTemplate
    {
        public string Name;
        public List<Limb> bodyMap;//it's for damage calculation
        public List<Organ> allOrgans;//it's for life calls
        public IEntity Owner;

        //TODO: blood should be placed here too, separated from limbs, but bones i think should be inside limb class.

        public virtual void ExposeData(ObjectSerializer obj)
        {
            //obj.DataField(ref bodyMap, "limbs", null); TODO: soon.
            //obj.DataField(ref neededFunctions, "bodyFunctions", null);
        }

        public virtual void Initialize(IEntity owner)
        {
            Owner = owner;
        }

        public void Life() //this is main Life() proc!
        {
            foreach(var organ in allOrgans)
            {
                organ.Life();
            }
        }
        //TODO: calculate damage...
    }
}
