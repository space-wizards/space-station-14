#nullable enable
using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class CombatModeSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class SetTargetZoneMessage : EntitySystemMessage
        {
            public SetTargetZoneMessage(TargetingZone targetZone)
            {
                TargetZone = targetZone;
            }

            public TargetingZone TargetZone { get; }
        }

        [Serializable, NetSerializable]
        public sealed class SetCombatModeActiveMessage : EntitySystemMessage
        {
            public SetCombatModeActiveMessage(bool active)
            {
                Active = active;
            }

            public bool Active { get; }
        }
    }
}
