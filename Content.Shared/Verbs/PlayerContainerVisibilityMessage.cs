using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Verbs
{
    [Serializable, NetSerializable]
    public class PlayerContainerVisibilityMessage : EntityEventArgs
    {
        public readonly bool CanSeeThrough;

        public PlayerContainerVisibilityMessage(bool canSeeThrough)
        {
            CanSeeThrough = canSeeThrough;
        }
    }
}
