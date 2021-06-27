using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "Explosive";

        [DataField("devastationRange")]
        public int DevastationRange;
        [DataField("heavyImpactRange")]
        public int HeavyImpactRange;
        [DataField("lightImpactRange")]
        public int LightImpactRange;
        [DataField("flashRange")]
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
                Owner.QueueDelete();
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
