using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposalTagGunComponent : Component
    {
        [Serializable, NetSerializable]
        public class TagChangedMessage : BoundUserInterfaceMessage
        {
            public readonly string Tag;

            public TagChangedMessage(string tag)
            {
                Tag = tag;
            }
        }

        [Serializable, NetSerializable]
        public enum DisposalTagGunUIKey
        {
            Key
        }
    }


}
