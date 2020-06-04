using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
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
    /// <summary>
    ///      Guns that have a magazine.
    /// </summary>
    [RegisterComponent]
    public class BallisticMagazineWeaponComponent : BallisticWeaponComponent, IUse, IInteractUsing, IMapInit
    {
        private const float BulletOffset = 0.2f;

        public override string Name => "BallisticMagazineWeapon";
        public override uint? NetID => ContentNetIDs.BALLISTIC_MAGAZINE_WEAPON;

        [ViewVariables] private string _defaultMagazine;

        [ViewVariables] private ContainerSlot _magazineSlot;
        private List<BallisticMagazineType> _magazineTypes;

        [ViewVariables] public List<BallisticMagazineType> MagazineTypes => _magazineTypes;
        [ViewVariables] private IEntity Magazine => _magazineSlot.ContainedEntity;

#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _bulletDropRandom;
#pragma warning restore 649
        [ViewVariables] private string _magInSound;
        [ViewVariables] private string _magOutSound;
        [ViewVariables] private string _autoEjectSound;
        [ViewVariables] private bool _autoEjectMagazine;
        [ViewVariables] private AppearanceComponent _appearance;

        private static readonly Direction[] RandomBulletDirs =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _magazineTypes, "magazines",
                new List<BallisticMagazineType> {BallisticMagazineType.Unspecified});
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
                Magazine.GetComponent<BallisticMagazineComponent>().OnAmmoCountChanged += MagazineAmmoCountChanged;
            }
            UpdateAppearance();
        }

        public bool InsertMagazine(IEntity magazine, bool playSound = true)
        {
            if (!magazine.TryGetComponent(out BallisticMagazineComponent magazinetype))
            {
                throw new ArgumentException("Not a magazine", nameof(magazine));
            }
            if (!MagazineTypes.Contains(magazinetype.MagazineType))
            {
                throw new ArgumentException("Wrong magazine type", nameof(magazine));
            }
            if (!_magazineSlot.Insert(magazine))
            {
                return false;
            }
            if (_magInSound != null && playSound)
            {
                EntitySystem.Get<AudioSystem>().Play(_magInSound, Owner);
            }
            magazinetype.OnAmmoCountChanged += MagazineAmmoCountChanged;
            if (GetChambered(0) == null)
            {
                // No bullet in chamber, load one from magazine.
                var bullet = magazinetype.TakeBullet();
                if (bullet != null)
                {
                    LoadIntoChamber(0, bullet);
                }
            }
            UpdateAppearance();
            Dirty();
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
                if (_magOutSound != null && playSound)
                {
                    EntitySystem.Get<AudioSystem>().Play(_magOutSound, Owner, AudioParams.Default.WithVolume(20));
                }
                UpdateAppearance();
                Dirty();
                entity.GetComponent<BallisticMagazineComponent>().OnAmmoCountChanged -= MagazineAmmoCountChanged;
                return true;
            }
            UpdateAppearance();
            Dirty();
            return false;
        }

        protected override void CycleChamberedBullet(int chamber)
        {
            DebugTools.Assert(chamber == 0);

            // Eject chambered bullet.
            var entity = RemoveFromChamber(chamber);
            if (entity == null)
            {
                return;
            }
            var offsetPos = (CalcBulletOffset(), CalcBulletOffset());
            entity.Transform.GridPosition = Owner.Transform.GridPosition.Offset(offsetPos);
            entity.Transform.LocalRotation = _bulletDropRandom.Pick(RandomBulletDirs).ToAngle();
            var effect = $"/Audio/Guns/Casings/casingfall{_bulletDropRandom.Next(1, 4)}.ogg";
            EntitySystem.Get<AudioSystem>().Play(effect, Owner, AudioParams.Default.WithVolume(-3));

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
                    DoAutoEject();
                }
            }
            Dirty();
            UpdateAppearance();
        }

        private float CalcBulletOffset()
        {
            return _bulletDropRandom.NextFloat() * (BulletOffset * 2) - BulletOffset;
        }

        private void DoAutoEject()
        {
            SendNetworkMessage(new BmwComponentAutoEjectedMessage());
            EjectMagazine();
            if (_autoEjectSound != null)
            {
                EntitySystem.Get<AudioSystem>().Play(_autoEjectSound, Owner, AudioParams.Default.WithVolume(-5));
            }
            Dirty();
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

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out BallisticMagazineComponent component))
            {
                return false;
            }
            if (Magazine != null)
            {
                Owner.PopupMessage(eventArgs.User, "Already got a magazine.");
                return false;
            }
            if (!MagazineTypes.Contains(component.MagazineType))
            {
                Owner.PopupMessage(eventArgs.User, "Magazine doesn't fit.");
                return false;
            }
            return InsertMagazine(eventArgs.Using);
        }

        private void MagazineAmmoCountChanged()
        {
            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
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

        public override ComponentState GetComponentState()
        {
            var chambered = GetChambered(0) != null;
            (int, int)? count = null;
            if (Magazine != null)
            {
                var magComponent = Magazine.GetComponent<BallisticMagazineComponent>();
                count = (magComponent.CountLoaded, magComponent.Capacity);
            }
            return new BallisticMagazineWeaponComponentState(chambered, count);
        }

        [Verb]
        public sealed class EjectMagazineVerb : Verb<BallisticMagazineWeaponComponent>
        {
            protected override void GetData(IEntity user, BallisticMagazineWeaponComponent component, VerbData data)
            {
                if (component.Magazine == null)
                {
                    data.Text = "Eject magazine (magazine missing)";
                    data.Visibility = VerbVisibility.Disabled;
                    return;
                }

                data.Text = "Eject magazine";
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
