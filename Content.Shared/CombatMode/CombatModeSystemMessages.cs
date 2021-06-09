#nullable enable
using System;
using Content.Shared.Targeting;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.CombatMode
{
    public static class CombatModeSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class SetTargetZoneMessage : EntityEventArgs
        {
            public SetTargetZoneMessage(TargetingZone targetZone)
            {
                TargetZone = targetZone;
            }

            public TargetingZone TargetZone { get; }
        }

        [Serializable, NetSerializable]
        public sealed class SetCombatModeActiveMessage : EntityEventArgs
        {
            public SetCombatModeActiveMessage(bool active)
            {
                Active = active;
            }

            public bool Active { get; }
        }
    }
}
