using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using JetBrains.Annotations;

namespace Content.Server.Damage.Systems
{
    [UsedImplicitly]
    public sealed class GodmodeSystem : EntitySystem
    {
        private readonly Dictionary<EntityUid, OldEntityInformation> _entities = new();
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _entities.Clear();
        }

        public bool EnableGodmode(EntityUid entity)
        {
            if (_entities.ContainsKey(entity))
            {
                return false;
            }

            _entities[entity] = new OldEntityInformation(entity, EntityManager);

            if (EntityManager.TryGetComponent(entity, out MovedByPressureComponent? moved))
            {
                moved.Enabled = false;
            }

            if (EntityManager.TryGetComponent(entity, out DamageableComponent? damageable))
            {
                _damageableSystem.SetDamage(damageable, new DamageSpecifier());
            }

            return true;
        }

        public bool HasGodmode(EntityUid entity)
        {
            return _entities.ContainsKey(entity);
        }

        public bool DisableGodmode(EntityUid entity)
        {
            if (!_entities.Remove(entity, out var old))
            {
                return false;
            }

            if (EntityManager.TryGetComponent(entity, out MovedByPressureComponent? moved))
            {
                moved.Enabled = old.MovedByPressure;
            }

            if (EntityManager.TryGetComponent(entity, out DamageableComponent? damageable))
            {
                if (old.Damage != null)
                {
                    _damageableSystem.SetDamage(damageable, old.Damage);
                }
            }

            return true;
        }

        /// <summary>
        ///     Toggles godmode for a given entity.
        /// </summary>
        /// <param name="entity">The entity to toggle godmode for.</param>
        /// <returns>true if enabled, false if disabled.</returns>
        public bool ToggleGodmode(EntityUid entity)
        {
            if (HasGodmode(entity))
            {
                DisableGodmode(entity);
                return false;
            }
            else
            {
                EnableGodmode(entity);
                return true;
            }
        }

        public sealed class OldEntityInformation
        {
            public OldEntityInformation(EntityUid entity, IEntityManager entityManager)
            {
                Entity = entity;
                MovedByPressure = entityManager.HasComponent<MovedByPressureComponent>(entity);

                if (entityManager.TryGetComponent(entity, out DamageableComponent? damageable))
                {
                    Damage = damageable.Damage;
                }
            }

            public EntityUid Entity { get; }

            public bool MovedByPressure { get; }

            public DamageSpecifier? Damage { get; }
        }
    }
}
