using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    [RegisterComponent]
    public class AmmoBoxComponent : Component, IAttackBy, IMapInit
    // TODO: Potential improvements:
    // Add verbs for stack splitting
    // Behaviour is largely the same as BallisticMagazine except you can't insert it into a gun.
    {
        public override string Name => "AmmoBox";
        private BallisticCaliber _caliber;
        private int _capacity;
        [ViewVariables] private int _availableSpawnCount;

        [ViewVariables] private readonly Stack<IEntity> _loadedBullets = new Stack<IEntity>();

        [ViewVariables]
        public string FillType => _fillType;
        private string _fillType;

        [ViewVariables] private Container _bulletContainer;
        [ViewVariables] private AppearanceComponent _appearance;

        [ViewVariables] public int Capacity => _capacity;
        [ViewVariables] public BallisticCaliber Caliber => _caliber;
        [ViewVariables] public int CountLeft => _loadedBullets.Count + _availableSpawnCount;


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _fillType, "fill", null);
            serializer.DataField(ref _capacity, "capacity", 30);
            serializer.DataField(ref _availableSpawnCount, "availableSpawnCount", Capacity);
        }

        private void _updateAppearance()
        {
            _appearance.SetData(BallisticMagazineVisuals.AmmoLeft, CountLeft);
        }

        public void MapInit()
        {
            _availableSpawnCount = Capacity;
        }

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            _bulletContainer =
                ContainerManagerComponent.Ensure<Container>("box_bullet_container", Owner, out var existed);

            if (existed)
            {
                foreach (var entity in _bulletContainer.ContainedEntities)
                {
                    _loadedBullets.Push(entity);
                }
            }

            _updateAppearance();
            _appearance.SetData(BallisticMagazineVisuals.AmmoCapacity, Capacity);
        }

        AmmoBoxTransferPopupMessage CanTransferFrom(IEntity source)
        {
            // Currently the below duplicates mags but at some stage these will likely differ
            if (source.TryGetComponent(out BallisticMagazineComponent magazineComponent))
            {
                if (magazineComponent.Caliber != Caliber)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "Wrong caliber");
                }

                if (CountLeft == Capacity)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "Already full");
                }

                if (magazineComponent.CountLoaded == 0)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "No ammo to transfer");
                }

                return new AmmoBoxTransferPopupMessage(result: true, message: "");
            }

            if (source.TryGetComponent(out AmmoBoxComponent boxComponent))
            {
                if (boxComponent.Caliber != Caliber)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "Wrong caliber");
                }

                if (CountLeft == Capacity)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "Already full");
                }

                if (boxComponent.CountLeft == 0)
                {
                    return new AmmoBoxTransferPopupMessage(result: false, message: "No ammo to transfer");
                }

                return new AmmoBoxTransferPopupMessage(result: true, message: "");
            }

            return new AmmoBoxTransferPopupMessage(result: false, message: "");
        }

        // TODO: Potentially abstract out to reduce duplicate structs
        private struct AmmoBoxTransferPopupMessage
        {
            public readonly bool Result;
            public readonly string Message;

            public AmmoBoxTransferPopupMessage(bool result, string message)
            {
                Result = result;
                Message = message;
            }
        }

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            var ammoBoxTransfer = CanTransferFrom(eventArgs.AttackWith);
            if (ammoBoxTransfer.Result) {
                IEntity bullet;
                if (eventArgs.AttackWith.TryGetComponent(out BallisticMagazineComponent magazineComponent))
                {
                    int fillCount = Math.Min(magazineComponent.CountLoaded, Capacity - CountLeft);
                    for (int i = 0; i < fillCount; i++)
                    {
                        bullet = magazineComponent.TakeBullet();
                        AddBullet(bullet);
                    }
                    eventArgs.User.PopupMessage(eventArgs.User, $"Transferred {fillCount} rounds");
                    return true;
                }
                if (eventArgs.AttackWith.TryGetComponent(out AmmoBoxComponent boxComponent))
                {
                    int fillCount = Math.Min(boxComponent.CountLeft, Capacity - CountLeft);
                    for (int i = 0; i < fillCount; i++)
                    {
                        bullet = boxComponent.TakeBullet();
                        AddBullet(bullet);
                    }
                    eventArgs.User.PopupMessage(eventArgs.User, $"Transferred {fillCount} rounds");
                    return true;
                }
            }
            else
            {
                eventArgs.User.PopupMessage(eventArgs.User, ammoBoxTransfer.Message);
            }

            return false;
        }

        public void AddBullet(IEntity bullet)
        {
            if (Owner.TryGetComponent(out BallisticMagazineComponent magazineComponent))
            {
                magazineComponent.AddBullet(bullet);
                return;
            }
            if (!bullet.TryGetComponent(out BallisticBulletComponent component))
            {
                throw new ArgumentException("entity isn't a bullet.", nameof(bullet));
            }

            if (component.Caliber != Caliber)
            {
                throw new ArgumentException("entity is of the wrong caliber.", nameof(bullet));
            }

            if (CountLeft >= Capacity)
            {
                throw new InvalidOperationException("Box is full.");
            }

            _bulletContainer.Insert(bullet);
            _loadedBullets.Push(bullet);
            _updateAppearance();
        }

        public IEntity TakeBullet()
        {
            IEntity bullet;
            if (Owner.TryGetComponent(out BallisticMagazineComponent magazineComponent))
            {
                bullet = magazineComponent.TakeBullet();
                return bullet;
            }
            if (_loadedBullets.Count == 0)
            {
                if (_availableSpawnCount == 0)
                {
                    return null;
                }

                _availableSpawnCount -= 1;
                bullet = Owner.EntityManager.SpawnEntity(FillType, Owner.Transform.GridPosition);
            }
            else
            {
                bullet = _loadedBullets.Pop();
                _bulletContainer.Remove(bullet);
            }

            _updateAppearance();
            return bullet;
        }

    }
}
