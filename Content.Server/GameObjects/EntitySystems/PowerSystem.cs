using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class PowerSystem : EntitySystem
    {
        public List<Powernet> Powernets = new List<Powernet>();

        private IComponentManager componentManager;

        private int _lastUid = 0;

        public PowerSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PowerDeviceComponent));
        }

        public override void Initialize()
        {
            base.Initialize();
            componentManager = IoCManager.Resolve<IComponentManager>();
        }

        public int NewUid()
        {
            return ++_lastUid;
        }

        public override void Update(float frametime)
        {
            for (int i = 0; i < Powernets.Count; i++)
            {
                var powernet = Powernets[i];
                if (powernet.Dirty)
                {
                    //Tell all the wires of this net to be prepared to create/join new powernets
                    foreach (var wire in powernet.WireList)
                    {
                        wire.Regenerating = true;
                    }

                    foreach (var wire in powernet.WireList)
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

            foreach (var powernet in Powernets)
            {
                powernet.Update(frametime);
            }

            // Draw power for devices not connected to anything.
            foreach (var entity in EntityManager.GetEntities(EntityQuery))
            {
                var device = entity.GetComponent<PowerDeviceComponent>();
                device.ProcessInternalPower(frametime);
            }
        }
    }
}
