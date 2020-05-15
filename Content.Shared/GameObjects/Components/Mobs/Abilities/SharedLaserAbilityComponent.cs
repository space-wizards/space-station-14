using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs.Abilities
{
    public abstract class SharedLaserAbilityComponent : Component
    {
        public override string Name => "LaserAbility";
        public override uint? NetID => ContentNetIDs.LASER_ABILITY;

        [Serializable, NetSerializable]
        public class FireLaserMessage : ComponentMessage
        {
            public GridCoordinates Coordinates;

            public FireLaserMessage(GridCoordinates coordinates)
            {
                Coordinates = coordinates;
            }
        }

        [Serializable, NetSerializable]
        public class FireLaserCooldownMessage : ComponentMessage
        {
            public TimeSpan Start;
            public TimeSpan End;

            public FireLaserCooldownMessage(TimeSpan start, TimeSpan end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
