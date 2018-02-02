using Content.Server.GameObjects.Components.Power;
using SS14.Shared.GameObjects.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class PowerSystem : EntitySystem
    {
        public List<Powernet> Powernets = new List<Powernet>();

        public override void Update(float frametime)
        {
            for (int i = 0; i < Powernets.Count; i++)
            {
                var powernet = Powernets[i];
                if (powernet.Dirty)
                {
                    //Tell all the wires of this net to be prepared to create/join new powernets
                    foreach (var wire in powernet.Wirelist)
                    {
                        wire.Regenerating = true;
                    }

                    foreach (var wire in powernet.Wirelist)
                    {
                        //Only a few wires should pass this if check since each will create and take all the others into its powernet
                        if (wire.Regenerating)
                            wire.SpreadPowernet();
                    }

                    //At this point all wires will have found/joined new powernet, all capable nodes will have joined them as well and removed themselves from nodelist
                    powernet.DirtyKill();
                    i--;
                }
            }

            foreach(var powernet in Powernets)
            {
                powernet.Update(frametime);
            }
        }
    }
}
