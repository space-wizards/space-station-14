using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class MeleeWeaponSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class PlayWeaponArcMessage : EntitySystemMessage
        {
            public PlayWeaponArcMessage(string arcPrototype, GridCoordinates position, Angle angle)
            {
                ArcPrototype = arcPrototype;
                Position = position;
                Angle = angle;
            }

            public string ArcPrototype { get; }
            public GridCoordinates Position { get; }
            public Angle Angle { get; }
        }
    }
}
