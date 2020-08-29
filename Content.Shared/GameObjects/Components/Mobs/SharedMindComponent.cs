using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public class SharedMindComponent : Component
    {
        public override string Name => "MindComponent";


        [Serializable, NetSerializable]
        public class BeingClonedMessage: ComponentMessage

        {
            public BeingClonedMessage(int cloningIndex)
            {
                CloningIndex = cloningIndex;
            }
            public int CloningIndex { get; }

        }
    }
}
