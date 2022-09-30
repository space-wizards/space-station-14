using Content.Shared.Targeting;
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
    }
}
