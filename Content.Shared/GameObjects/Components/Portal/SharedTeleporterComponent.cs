using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Portal
{
    public enum ItemTeleporterState : byte
    {
        Off,
        Charging,
        On,
        Cooldown,
    }

    public enum TeleporterType : byte
    {
        Directed,
        Random,
        Beacon,
    }

    [NetSerializable]
    [Serializable]
    public enum TeleporterVisuals : byte
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum TeleporterVisualState : byte
    {
        Ready,
        Charging,
    }

    [Serializable, NetSerializable]
    public class TeleportMessage : ComponentMessage
    {
        public readonly EntityCoordinates Target;

        public TeleportMessage(EntityCoordinates target)
        {
            Target = target;
        }
    }
}
