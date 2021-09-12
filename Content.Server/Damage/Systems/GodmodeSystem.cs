using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems
{
    [UsedImplicitly]
    public class GodmodeSystem : EntitySystem
    {
        private readonly Dictionary<IEntity, OldEntityInformation> _entities = new();
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

        public bool EnableGodmode(IEntity entity)
        {
            if (_entities.ContainsKey(entity))
            {
                return false;
            }

            _entities[entity] = new OldEntityInformation(entity);

            if (entity.TryGetComponent(out MovedByPressureComponent? moved))
            {
                moved.Enabled = false;
            }

            if (entity.TryGetComponent(out DamageableComponent? damageable))
            {
                _damageableSystem.SetDamage(damageable, new DamageSpecifier());
            }

            return true;
        }

        public bool HasGodmode(IEntity entity)
        {
            return _entities.ContainsKey(entity);
        }

        public bool DisableGodmode(IEntity entity)
        {
            if (!_entities.Remove(entity, out var old))
            {
                return false;
            }

            if (entity.TryGetComponent(out MovedByPressureComponent? moved))
            {
                moved.Enabled = old.MovedByPressure;
            }

            if (entity.TryGetComponent(out DamageableComponent? damageable))
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
        public bool ToggleGodmode(IEntity entity)
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

        public class OldEntityInformation
        {
            public OldEntityInformation(IEntity entity)
            {
                Entity = entity;
                MovedByPressure = entity.IsMovedByPressure();

                if (entity.TryGetComponent(out DamageableComponent? damageable))
                {
                    Damage = damageable.Damage;
                }
            }

            public IEntity Entity { get; }

            public bool MovedByPressure { get; }

            public DamageSpecifier? Damage { get; }
        }
    }
}
