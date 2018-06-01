using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Content.Server.GameObjects.EntitySystems
{
    public class StorageSystem : EntitySystem
    {
        public List<ServerStorageComponent> StoringComponents = new List<ServerStorageComponent>();
        private IComponentManager componentManager;

        public override void Initialize()
        {
            base.Initialize();
            componentManager = IoCManager.Resolve<IComponentManager>();
        }

        public override void Update(float frameTime)
        {
            foreach (ServerStorageComponent StorageComponent in StoringComponents)
            {
                StorageComponent.ValidateChannels();
            }
        }
    }
}

