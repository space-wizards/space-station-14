using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    [RegisterComponent]
    public class BallisticMagazineWeaponComponent : BallisticWeaponComponent, IUse, IAttackBy, IMapInit
    {
        public override string Name => "BallisticMagazineWeapon";

        [ViewVariables]
        private string _defaultMagazine;

        [ViewVariables]
        private ContainerSlot _magazineSlot;
        private List<BallisticMagazineType> _magazineTypes;

        [ViewVariables]
        public List<BallisticMagazineType> MagazineTypes => _magazineTypes;
        [ViewVariables]
        private IEntity Magazine => _magazineSlot.ContainedEntity;

#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _bulletDropRandom;
#pragma warning restore 649
        [ViewVariables]
        private string _magInSound;
        [ViewVariables]
        private string _magOutSound;
        [ViewVariables]
        private string _autoEjectSound;
        [ViewVariables]
        private bool _autoEjectMagazine;
        [ViewVariables]
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

            serializer.DataField(ref _magazineTypes, "magazines",
                            new List<BallisticMagazineType>{BallisticMagazineType.Unspecified});
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
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            _magazineSlot = ContainerManagerComponent.Ensure<ContainerSlot>("ballistic_gun_magazine", Owner);

            if (Magazine != null)
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

            if (!MagazineTypes.Contains(component.MagazineType))
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
                Owner.GetComponent<SoundComponent>().Play(_magInSound);
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
                    Owner.GetComponent<SoundComponent>().Play(_magOutSound);
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
            var effect = $"/Audio/Guns/Casings/casingfall{_bulletDropRandom.Next(1, 4)}.ogg";
            Owner.GetComponent<SoundComponent>().Play(effect, AudioParams.Default.WithVolume(-3));

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
                        Owner.GetComponent<SoundComponent>().Play(_autoEjectSound, AudioParams.Default.WithVolume(-5));
                    }
                }
            }

            _updateAppearance();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            var ret = EjectMagazine();
            if (ret)
            {
                Owner.PopupMessage(eventArgs.User, "Magazine ejected");
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, "No magazine");
            }

            return true;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.TryGetComponent(out BallisticMagazineComponent component))
            {
                return false;
            }

            if (Magazine != null)
            {
                Owner.PopupMessage(eventArgs.User, "Already got a magazine.");
                return false;
            }

            if (!MagazineTypes.Contains(component.MagazineType) || component.Caliber != Caliber)
            {
                Owner.PopupMessage(eventArgs.User, "Magazine doesn't fit.");
                return false;
            }

            return InsertMagazine(eventArgs.AttackWith);
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

            protected override VerbVisibility GetVisibility(IEntity user, BallisticMagazineWeaponComponent component)
            {
                return component.Magazine == null ? VerbVisibility.Disabled : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, BallisticMagazineWeaponComponent component)
            {
                component.EjectMagazine();
            }
        }

        void IMapInit.MapInit()
        {
            if (_defaultMagazine != null)
            {
                var magazine = Owner.EntityManager.SpawnEntity(_defaultMagazine, Owner.Transform.GridPosition);
                InsertMagazine(magazine, false);
            }
        }
    }
}
