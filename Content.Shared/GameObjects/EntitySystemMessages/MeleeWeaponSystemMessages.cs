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
        public sealed class PlayMeleeWeaponAnimationMessage : EntitySystemMessage
        {
            public PlayMeleeWeaponAnimationMessage(string arcPrototype, Angle angle, EntityUid attacker, EntityUid source, List<EntityUid> hits, bool textureEffect = false)
            {
                ArcPrototype = arcPrototype;
                Angle = angle;
                Attacker = attacker;
                Source = source;
                Hits = hits;
                TextureEffect = textureEffect;
            }

            public string ArcPrototype { get; }
            public Angle Angle { get; }
            public EntityUid Attacker { get; }
            public EntityUid Source { get; }
            public List<EntityUid> Hits { get; }
            public bool TextureEffect { get; }
        }
    }
}
