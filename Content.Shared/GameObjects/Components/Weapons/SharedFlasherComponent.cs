using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons
{
    public abstract class SharedFlasherComponent : Component
    {
        public override string Name => "Flasher";
        public override uint? NetID => ContentNetIDs.FLASHER;
    }

    [Serializable, NetSerializable]
    public class FlasherComponentMessage : ComponentMessage {}
}
