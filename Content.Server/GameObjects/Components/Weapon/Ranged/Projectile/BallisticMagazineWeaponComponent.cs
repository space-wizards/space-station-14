using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Interfaces;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    public class BallisticMagazineWeaponComponent : BallisticWeaponComponent, IUse, IAttackby
    {
        public override string Name => "BallisticMagazineWeapon";

        private string _defaultMagazine;

        private ContainerSlot _magazineSlot;
        private BallisticMagazineType _magazineType;

        public BallisticMagazineType MagazineType => _magazineType;
        private IEntity Magazine => _magazineSlot.ContainedEntity;

        private Random _bulletDropRandom;
        private string _magInSound;
        private string _magOutSound;
        private string _autoEjectSound;
        private bool _autoEjectMagazine;
        private AppearanceComponent _appearance;

        private static readonly Direction[] _randomBulletDirs =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        protected override int ChamberCount => 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _magazineType, "magazine", BallisticMagazineType.Unspecified);
            serializer.DataField(ref _defaultMagazine, "default_magazine", null);
            serializer.DataField(ref _autoEjectMagazine, "auto_eject_magazine", false);
            serializer.DataField(ref _autoEjectSound, "sound_auto_eject", null);
            serializer.DataField(ref _magInSound, "sound_magazine_in", null);
            serializer.DataField(ref _magOutSound, "sound_magazine_out", null);
        }

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
            _bulletDropRandom = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
        }

        public override void Startup()
        {
            base.Startup();

            _magazineSlot =
                ContainerManagerComponent.Ensure<ContainerSlot>("ballistic_gun_magazine", Owner,
                    out var alreadyExisted);

            if (!alreadyExisted && _defaultMagazine != null)
            {
                var magazine = Owner.EntityManager.SpawnEntity(_defaultMagazine);
                InsertMagazine(magazine, false);
            }
            else if (Magazine != null)
            {
                // Already got magazine from loading a container.
                Magazine.GetComponent<BallisticMagazineComponent>().OnAmmoCountChanged += _magazineAmmoCountChanged;
            }

            _updateAppearance();
        }

        public bool InsertMagazine(IEntity magazine, bool playSound = true)
        {
            if (!magazine.TryGetComponent(out BallisticMagazineComponent component))
            {
                throw new ArgumentException("Not a magazine", nameof(magazine));
            }

            if (component.MagazineType != MagazineType)
            {
                throw new ArgumentException("Wrong magazine type", nameof(magazine));
            }

            if (component.Caliber != Caliber)
            {
                throw new ArgumentException("Wrong caliber", nameof(magazine));
            }

            if (!_magazineSlot.Insert(magazine))
            {
                return false;
            }

            if (_magInSound != null)
            {
                var audioSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
                audioSystem.Play(_magInSound, Owner);
            }

            component.OnAmmoCountChanged += _magazineAmmoCountChanged;
            if (GetChambered(0) == null)
            {
                // No bullet in chamber, load one from magazine.
                var bullet = component.TakeBullet();
                if (bullet != null)
                {
                    LoadIntoChamber(0, bullet);
                }
            }

            _updateAppearance();
            return true;
        }

        public bool EjectMagazine(bool playSound = true)
        {
            var entity = Magazine;
            if (entity == null)
            {
                return false;
            }

            if (_magazineSlot.Remove(entity))
            {
                entity.Transform.GridPosition = Owner.Transform.GridPosition;
                if (_magOutSound != null)
                {
                    var audioSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
                    audioSystem.Play(_magOutSound, Owner);
                }

                _updateAppearance();
                entity.GetComponent<BallisticMagazineComponent>().OnAmmoCountChanged -= _magazineAmmoCountChanged;
                return true;
            }

            _updateAppearance();
            return false;
        }

        protected override void CycleChamberedBullet(int chamber)
        {
            DebugTools.Assert(chamber == 0);

            // Eject chambered bullet.
            var entity = RemoveFromChamber(chamber);
            entity.Transform.GridPosition = Owner.Transform.GridPosition;
            entity.Transform.LocalRotation = _bulletDropRandom.Pick(_randomBulletDirs).ToAngle();
            var audioSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
            var effect = $"/Audio/items/weapons/casingfall{_bulletDropRandom.Next(1, 4)}.ogg";
            audioSystem.Play(effect, entity, AudioParams.Default.WithVolume(-3));

            if (Magazine != null)
            {
                var magComponent = Magazine.GetComponent<BallisticMagazineComponent>();
                var bullet = magComponent.TakeBullet();
                if (bullet != null)
                {
                    LoadIntoChamber(0, bullet);
                }

                if (magComponent.CountLoaded == 0 && _autoEjectMagazine)
                {
                    EjectMagazine();
                    if (_autoEjectSound != null)
                    {
                        audioSystem.Play(_autoEjectSound, Owner, AudioParams.Default.WithVolume(-5));
                    }
                }
            }

            _updateAppearance();
        }

        public bool UseEntity(IEntity user)
        {
            var ret = EjectMagazine();
            if (ret)
            {
                Owner.PopupMessage(user, "Magazine ejected");
            }
            else
            {
                Owner.PopupMessage(user, "No magazine");
            }

            return true;
        }

        public bool Attackby(IEntity user, IEntity attackwith)
        {
            if (!attackwith.TryGetComponent(out BallisticMagazineComponent component))
            {
                return false;
            }

            if (Magazine != null)
            {
                Owner.PopupMessage(user, "Already got a magazine.");
                return false;
            }

            if (component.MagazineType != MagazineType || component.Caliber != Caliber)
            {
                Owner.PopupMessage(user, "Magazine doesn't fit.");
                return false;
            }

            return InsertMagazine(attackwith);
        }

        private void _magazineAmmoCountChanged()
        {
            _updateAppearance();
        }

        private void _updateAppearance()
        {
            if (Magazine != null)
            {
                var comp = Magazine.GetComponent<BallisticMagazineComponent>();
                _appearance.SetData(BallisticMagazineWeaponVisuals.AmmoLeft, comp.CountLoaded);
                _appearance.SetData(BallisticMagazineWeaponVisuals.AmmoCapacity, comp.Capacity);
                _appearance.SetData(BallisticMagazineWeaponVisuals.MagazineLoaded, true);
            }
            else
            {
                _appearance.SetData(BallisticMagazineWeaponVisuals.AmmoLeft, 0);
                _appearance.SetData(BallisticMagazineWeaponVisuals.AmmoLeft, 0);
                _appearance.SetData(BallisticMagazineWeaponVisuals.MagazineLoaded, false);
            }
        }

        [Verb]
        public sealed class EjectMagazineVerb : Verb<BallisticMagazineWeaponComponent>
        {
            protected override string GetText(IEntity user, BallisticMagazineWeaponComponent component)
            {
                return component.Magazine == null ? "Eject magazine (magazine missing)" : "Eject magazine";
            }

            protected override bool IsDisabled(IEntity user, BallisticMagazineWeaponComponent component)
            {
                return component.Magazine == null;
            }

            protected override void Activate(IEntity user, BallisticMagazineWeaponComponent component)
            {
                component.EjectMagazine();
            }
        }
    }
}
