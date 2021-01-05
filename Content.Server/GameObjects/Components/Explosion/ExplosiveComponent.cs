using Content.Server.Explosions;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Explosion
{
    [RegisterComponent]
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "Explosive";

        [YamlField("devastationRange")]
        public int DevastationRange;
        [YamlField("heavyImpactRange")]
        public int HeavyImpactRange;
        [YamlField("lightImpactRange")]
        public int LightImpactRange;
        [YamlField("flashRange")]
        public int FlashRange;

        private bool _beingExploded = false;

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
