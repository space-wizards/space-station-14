using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    public class SharedPowerDebugTool : Component
    {
        public override string Name => "PowerDebugTool";
        public override uint? NetID => ContentNetIDs.POWER_DEBUG_TOOL;

        [Serializable, NetSerializable]
        protected class OpenDataWindowMsg : ComponentMessage
        {
            public string Data { get; }

            public OpenDataWindowMsg(string data)
            {
                Directed = true;
                Data = data;
            }
        }
    }
}
