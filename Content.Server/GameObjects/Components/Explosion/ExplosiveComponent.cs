using Content.Server.Explosions;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Random;
using System;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.Timing;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Content.Server.GameObjects.Components.Mobs;

namespace Content.Server.GameObjects.Components.Explosion
{
    [RegisterComponent]
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "Explosive";

        public int DevastationRange = 0;
        public int HeavyImpactRange = 0;
        public int LightImpactRange = 0;
        public int FlashRange = 0;

        public bool Exploding { get; private set; } = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref DevastationRange, "devastationRange", 0);
            serializer.DataField(ref HeavyImpactRange, "heavyImpactRange", 0);
            serializer.DataField(ref LightImpactRange, "lightImpactRange", 0);
            serializer.DataField(ref FlashRange, "flashRange", 0);
        }

        public bool Explosion()
        {
            if (Exploding)
            {
                return false;
            }
            else
            {
                Exploding = true;
                Owner.SpawnExplosion(DevastationRange, HeavyImpactRange, LightImpactRange, FlashRange);
                return true;
            }
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            return Explosion();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            Explosion();
        }
    }
}
