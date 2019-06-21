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
        public List<Limb> bodyMap;
        public List<Organ> allOrgans;
        public List<IBodyFunction> neededFunctions;
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

        public void Update()
        {
            foreach(var function in neededFunctions)
            {
                var ignoreNodes = new List<OrganNode>();
                if (ignoreNodes.Contains(function.Node)) {
                    continue; //no duplicates
                }
                foreach (var organ in allOrgans)
                {
                    if (organ.Nodes.Contains(function.Node))
                    {
                        ignoreNodes.Add(function.Node);
                        function.Life(Owner, organ.State); 
                    }
                }
            }
        }
        //TODO: calculate damage...
    }
}
