using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Content.Server.Explosions;

namespace Content.Server.GameObjects.Components.Explosive
{
    [RegisterComponent]
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

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

            ExplosionHelper.SpawnExplosion(Owner.Transform.GridPosition, DevastationRange, HeavyImpactRange, LightImpactRange, FlashRange);

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
