using Content.Server.Explosions;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

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

        private bool _beingExploded = false;

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
            //Prevent adjacent explosives from infinitely blowing each other up.
            if (_beingExploded) return true;
            _beingExploded = true;

            Owner.SpawnExplosion(DevastationRange, HeavyImpactRange, LightImpactRange, FlashRange);

            Owner.Delete();
            return true;
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
