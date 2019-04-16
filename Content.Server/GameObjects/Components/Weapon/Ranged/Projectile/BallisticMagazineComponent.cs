using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public class BallisticMagazineComponent : Component
    {
        public override string Name => "BallisticMagazine";

        // Stack of loaded bullets.
        private readonly Stack<IEntity> _loadedBullets = new Stack<IEntity>();
        private string _fillType;

        private Container _bulletContainer;
        private AppearanceComponent _appearance;

        private BallisticMagazineType _magazineType;
        private BallisticCaliber _caliber;
        private int _capacity;

        public BallisticMagazineType MagazineType => _magazineType;
        public BallisticCaliber Caliber => _caliber;
        public int Capacity => _capacity;

        public int CountLoaded => _loadedBullets.Count;

        public event Action OnAmmoCountChanged;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _magazineType, "magazine", BallisticMagazineType.Unspecified);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _fillType, "fill", null);
            serializer.DataField(ref _capacity, "capacity", 20);
        }

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
        }

        public override void Startup()
        {
            base.Startup();

            _bulletContainer =
                ContainerManagerComponent.Ensure<Container>("magazine_bullet_container", Owner, out var existed);

            if (existed)
            {
                foreach (var entity in _bulletContainer.ContainedEntities)
                {
                    _loadedBullets.Push(entity);
                }
                _updateAppearance();
            }
            else if (_fillType != null)
            {
                // Load up bullets from fill.
                for (var i = 0; i < Capacity; i++)
                {
                    var bullet = Owner.EntityManager.SpawnEntity(_fillType);
                    AddBullet(bullet);
                }
            }

            OnAmmoCountChanged?.Invoke();
            _appearance.SetData(BallisticMagazineVisuals.AmmoCapacity, Capacity);
        }

        public void AddBullet(IEntity bullet)
        {
            if (!bullet.TryGetComponent(out BallisticBulletComponent component))
            {
                throw new ArgumentException("entity isn't a bullet.", nameof(bullet));
            }

            if (component.Caliber != Caliber)
            {
                throw new ArgumentException("entity is of the wrong caliber.", nameof(bullet));
            }

            _bulletContainer.Insert(bullet);
            _loadedBullets.Push(bullet);
            _updateAppearance();
            OnAmmoCountChanged?.Invoke();
        }

        public IEntity TakeBullet()
        {
            if (_loadedBullets.Count == 0)
            {
                return null;
            }

            var bullet = _loadedBullets.Pop();
            _updateAppearance();
            OnAmmoCountChanged?.Invoke();
            return bullet;
        }

        private void _updateAppearance()
        {
            _appearance.SetData(BallisticMagazineVisuals.AmmoLeft, CountLoaded);
        }
    }

    public enum BallisticMagazineType
    {
        Unspecified = 0,
        A12mm,
    }
}
