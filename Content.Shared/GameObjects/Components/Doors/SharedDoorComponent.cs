using System;
using SS14.Shared.GameObjects;

namespace Content.Shared.GameObjects
{
    public abstract class SharedDoorComponent : Component
    {
        public override string Name => "Door";
        public override uint? NetID => ContentNetIDs.DOOR;
        public override Type StateType => typeof(DoorComponentState);
    }

    [Serializable]
    public class DoorComponentState : ComponentState
    {
        public readonly bool Opened;

        public DoorComponentState(bool opened) : base(ContentNetIDs.DOOR)
        {
            Opened = opened;
        }
    }
}
