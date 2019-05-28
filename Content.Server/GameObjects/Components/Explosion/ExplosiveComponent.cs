using Robust.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;
using Content.Server.GameObjects.Components.Destructible;
using Robust.Shared.Interfaces.GameObjects;
using System.Linq;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Explosive
{
    public class ExplosiveComponent : Component, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
#pragma warning restore 649

        public override string Name => "Explosive";

        public int DamageMax = 1;
        public int DamageFalloff = 1;
        public int RangeDamageMax = 1;
        public int Range = 1;
        public int Delay = 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref DamageMax, "damageMax", 1);
            serializer.DataField(ref DamageFalloff, "damageFalloff", 1);
            serializer.DataField(ref RangeDamageMax, "rangeDamageMax", 1);
            serializer.DataField(ref Range, "range", 1);
            serializer.DataField(ref Delay, "delay", 0);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            var location = eventArgs.User.Transform.GridPosition;

            var entitiesAll = _serverEntityManager.GetEntitiesInRange(location, Range);

            List<IEntity> entities = entitiesAll.ToList();

            foreach (var entity in entitiesAll)
            {
                var distance = (int)entity.Transform.GridPosition.Distance(_mapManager, location);
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User || distance > RangeDamageMax)
                    continue;
                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    damagecomponent.TakeDamage(DamageType.Brute, DamageMax);
                }
            }

            foreach (var entity in entities)
            {
                var distance = (int)entity.Transform.GridPosition.Distance(_mapManager, location);
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User || distance <= RangeDamageMax)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    
                    var overallDamageFalloff = DamageFalloff;
                    if (overallDamageFalloff > DamageMax) {
                        overallDamageFalloff = DamageMax;
                    }
                    damagecomponent.TakeDamage(DamageType.Brute, DamageMax - distance*overallDamageFalloff);
                }
            }
            return true;
        }
    }
}
