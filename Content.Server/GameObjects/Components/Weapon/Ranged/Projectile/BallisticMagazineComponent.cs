using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Serialization;

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

            _bulletContainer =
                ContainerManagerComponent.Ensure<Container>("magazine_bullet_container", Owner, out var existed);

            if (!existed && _fillType != null)
            {
                // Load up bullets from fill.
                for (var i = 0; i < Capacity; i++)
                {
                    var bullet = Owner.EntityManager.SpawnEntity(_fillType);
                    AddBullet(bullet);
                }
            }

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
        }

        public IEntity TakeBullet()
        {
            if (_loadedBullets.Count == 0)
            {
                return null;
            }

            var bullet = _loadedBullets.Pop();
            _updateAppearance();
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
