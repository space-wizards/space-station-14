using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class MeleeWeaponSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class PlayMeleeWeaponArcAnimationMessage : EntitySystemMessage
        {
            public PlayMeleeWeaponArcAnimationMessage(string arcPrototype, Angle angle, EntityUid attacker, List<EntityUid> hits)
            {
                ArcPrototype = arcPrototype;
                Angle = angle;
                Attacker = attacker;
                Hits = hits;
            }

            public string ArcPrototype { get; }
            public Angle Angle { get; }
            public EntityUid Attacker { get; }
            public List<EntityUid> Hits { get; }
        }

        [Serializable, NetSerializable]
        public sealed class PlayMeleeWeaponAnimationMessage : EntitySystemMessage
        {
            public PlayMeleeWeaponAnimationMessage(Angle angle, EntityUid attacker, EntityUid hit)
            {
                Attacker = attacker;
                Hit = hit;
            }

            public Angle Angle { get; }
            public EntityUid Attacker { get; }
            public EntityUid Hit { get; }
        }
    }
}
