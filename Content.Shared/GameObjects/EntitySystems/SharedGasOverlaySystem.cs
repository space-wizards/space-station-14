using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedGasOverlaySystem : EntitySystem
    {
        [Serializable, NetSerializable]
        public class GasOverlayMessage : EntitySystemMessage
        {
            
        }
    }
}
