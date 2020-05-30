using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.HUD.Hotbar
{
    public class SharedHotbarComponent : Component
    {
        public override string Name => "Hotbar";
        public override uint? NetID => ContentNetIDs.HOTBAR;
    }

    [Serializable, NetSerializable]
    public class HotbarActionMessage : ComponentMessage
    {
        public HotbarActionId Id { get; }
        public bool Enabled { get; }

        public HotbarActionMessage(HotbarActionId id, bool enabled)
        {
            Id = id;
            Enabled = enabled;
        }
    }

    public enum HotbarActionId
    {
        None,
        HandheldLight,
    }
}
