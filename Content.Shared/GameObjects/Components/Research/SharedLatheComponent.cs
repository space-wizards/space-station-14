using System;
using System.Collections.Generic;
using Content.Shared.Research;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public enum LatheType
    {
        Autolathe,
        Protolathe,
    }
    public class SharedLatheComponent : Component
    {
        public override string Name => "Lathe";
        public override uint? NetID => ContentNetIDs.LATHE;
        public LatheType LatheType = LatheType.Autolathe;

        public virtual bool CanProduce(LatheRecipePrototype recipe, uint quantity)
        {
            return false;
        }

        [Serializable, NetSerializable]
        public class LatheMenuOpenMessage : ComponentMessage
        {
            public LatheMenuOpenMessage()
            {
                Directed = true;
            }
        }
    }
}
