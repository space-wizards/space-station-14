using Content.Server.Explosions;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

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

        public bool Exploding { get; private set; } = false;

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
                Owner.Delete();
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
