using System;
using System.Linq;
using System.Collections.Generic;
using Content.Shared.Radiation;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Server.Singularity.Components;
using Content.Server.Power.Components;


namespace Content.Server.Radiation
{
    [UsedImplicitly]
    public sealed class RadiationSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [ComponentDependency] private readonly BatteryComponent? _batteryComponent = default!;

        private const float RadiationCooldown = 0.5f;
        private float _accumulator;
        public override void Initialize()
        {
            base.Initialize();
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }
        public List<string> RadiationDamageTypeIDs = new() {"Radiation"};
        public void Radiate(float Energy, float Range, IComponent Source, float frameTime)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            _accumulator += frameTime;

            DamageSpecifier _damage = new();
            PowerSupplierComponent _collector = new();

            while (_accumulator > RadiationCooldown)
            {
                _accumulator -= RadiationCooldown;
                foreach (var entity in _lookup.GetEntitiesInRange(Source.Owner.Transform.Coordinates, Range))
                {
                    float InRange = _lookup.GetEntitiesInRange(Source.Owner.Transform.Coordinates, Range).Count();
                    if (entity.HasComponent<DamageableComponent>())
                    {
                        //TODO: make damage falloff with range and add plasmaglass occluding
                        _damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Radiation"), (int) Energy/100);

                        foreach (var typeID in RadiationDamageTypeIDs)
                        _damageable.TryChangeDamage(entity.Uid, _damage);
                    }

                    if (entity.HasComponent<RadiationCollectorComponent>())
                    {
                        //TODO: make energy falloff with range and plasmaglass occluding
                        float supply = (Energy / InRange) * 1.25f;
                        entity.GetComponent<PowerSupplierComponent>().MaxSupply = supply;
                    }
                }
            }
        }
    }
}
