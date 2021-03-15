using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    [RegisterComponent]
    public sealed class ServerMagazineBarrelComponent : ServerRangedBarrelComponent, IExamine
    {
        public override string Name => "MagazineBarrel";
        public override uint? NetID => ContentNetIDs.MAGAZINE_BARREL;

        [ViewVariables]
        private ContainerSlot _chamberContainer;
        [ViewVariables] public bool HasMagazine => _magazineContainer.ContainedEntity != null;
        private ContainerSlot _magazineContainer;

        [ViewVariables] public MagazineType MagazineTypes => _magazineTypes;
        [DataField("magazineTypes")]
        private MagazineType _magazineTypes = default;
        [ViewVariables] public BallisticCaliber Caliber => _caliber;
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        public override int ShotsLeft
        {
            get
            {
                var count = 0;
                if (_chamberContainer.ContainedEntity != null)
                {
                    count++;
                }

                var magazine = _magazineContainer.ContainedEntity;
                if (magazine != null)
                {
                    count += magazine.GetComponent<RangedMagazineComponent>().ShotsLeft;
                }

                return count;
            }
        }

        public override int Capacity
        {
            get
            {
                // Chamber
                var count = 1;
                var magazine = _magazineContainer.ContainedEntity;
                if (magazine != null)
                {
                    count += magazine.GetComponent<RangedMagazineComponent>().Capacity;
                }

                return count;
            }
        }

        [DataField("magFillPrototype")]
        private string _magFillPrototype;

        public bool BoltOpen
        {
            get => _boltOpen;
            set
            {
                if (_boltOpen == value)
                {
                    return;
                }

                var soundSystem = EntitySystem.Get<AudioSystem>();

                if (value)
                {
                    TryEjectChamber();
                    if (_soundBoltOpen != null)
                    {
                        soundSystem.PlayAtCoords(_soundBoltOpen, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }
                else
                {
                    TryFeedChamber();
                    if (_soundBoltClosed != null)
                    {
                        soundSystem.PlayAtCoords(_soundBoltClosed, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }

                _boltOpen = value;
                UpdateAppearance();
                Dirty();
            }
        }
        private bool _boltOpen = true;

        [DataField("autoEjectMag")]
        private bool _autoEjectMag;
        // If the bolt needs to be open before we can insert / remove the mag (i.e. for LMGs)
        public bool MagNeedsOpenBolt => _magNeedsOpenBolt;
        [DataField("magNeedsOpenBolt")]
        private bool _magNeedsOpenBolt = default;

        private AppearanceComponent _appearanceComponent;

        // Sounds
        [DataField("soundBoltOpen")]
        private string _soundBoltOpen = default;
        [DataField("soundBoltClosed")]
        private string _soundBoltClosed = default;
        [DataField("soundRack")]
        private string _soundRack = default;
        [DataField("soundMagInsert")]
        private string _soundMagInsert = default;
        [DataField("soundMagEject")]
        private string _soundMagEject = default;
        [DataField("soundAutoEject")]
        private string _soundAutoEject = "/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg";

        private List<MagazineType> GetMagazineTypes()
        {
            var types = new List<MagazineType>();

            foreach (MagazineType mag in Enum.GetValues(typeof(MagazineType)))
            {
                if ((_magazineTypes & mag) != 0)
                {
                    types.Add(mag);
                }
            }

            return types;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            (int, int)? count = null;
            var magazine = _magazineContainer.ContainedEntity;
            if (magazine != null && magazine.TryGetComponent(out RangedMagazineComponent rangedMagazineComponent))
            {
                count = (rangedMagazineComponent.ShotsLeft, rangedMagazineComponent.Capacity);
            }

            return new MagazineBarrelComponentState(
                _chamberContainer.ContainedEntity != null,
                FireRateSelector,
                count,
                SoundGunshot);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _chamberContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-chamber");
            _magazineContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-magazine", out var existing);

            if (!existing && _magFillPrototype != null)
            {
                var magEntity = Owner.EntityManager.SpawnEntity(_magFillPrototype, Owner.Transform.Coordinates);
                _magazineContainer.Insert(magEntity);
            }
            Dirty();
        }

        protected override void Startup()
        {
            UpdateAppearance();
        }

        public override IEntity PeekAmmo()
        {
            return BoltOpen ? null : _chamberContainer.ContainedEntity;
        }

        public override IEntity TakeProjectile(EntityCoordinates spawnAt)
        {
            if (BoltOpen)
            {
                return null;
            }
            var entity = _chamberContainer.ContainedEntity;

            Cycle();
            return entity?.GetComponent<AmmoComponent>().TakeBullet(spawnAt);
        }

        private void Cycle(bool manual = false)
        {
            if (BoltOpen)
            {
                return;
            }

            TryEjectChamber();

            TryFeedChamber();

            var soundSystem = EntitySystem.Get<AudioSystem>();

            if (_chamberContainer.ContainedEntity == null && !BoltOpen)
            {
                if (_soundBoltOpen != null)
                {
                    soundSystem.PlayAtCoords(_soundBoltOpen, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-5));
                }

                if (Owner.TryGetContainer(out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("Bolt open"));
                }
                BoltOpen = true;
                return;
            }

            if (manual)
            {
                if (_soundRack != null)
                {
                    soundSystem.PlayAtCoords(_soundRack, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
            }

            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, _magazineContainer.ContainedEntity != null);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            // Behavior:
            // If bolt open just close it
            // If bolt closed then cycle
            //     If we cycle then get next round
            //         If no more round then open bolt

            if (BoltOpen)
            {
                if (_soundBoltClosed != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundBoltClosed, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-5));
                }
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Bolt closed"));
                BoltOpen = false;
                return true;
            }

            // Could play a rack-slide specific sound here if you're so inclined (if the chamber is empty but rounds are available)

            Cycle(true);
            return true;
        }

        public bool TryEjectChamber()
        {
            var chamberEntity = _chamberContainer.ContainedEntity;
            if (chamberEntity != null)
            {
                if (!_chamberContainer.Remove(chamberEntity))
                {
                    return false;
                }
                var ammoComponent = chamberEntity.GetComponent<AmmoComponent>();
                if (!ammoComponent.Caseless)
                {
                    EjectCasing(chamberEntity);
                }
                return true;
            }
            return false;
        }

        public bool TryFeedChamber()
        {
            if (_chamberContainer.ContainedEntity != null)
            {
                return false;
            }

            // Try and pull a round from the magazine to replace the chamber if possible
            var magazine = _magazineContainer.ContainedEntity;
            var nextRound = magazine?.GetComponent<RangedMagazineComponent>().TakeAmmo();

            if (nextRound == null)
            {
                return false;
            }

            _chamberContainer.Insert(nextRound);

            if (_autoEjectMag && magazine != null && magazine.GetComponent<RangedMagazineComponent>().ShotsLeft == 0)
            {
                if (_soundAutoEject != null)
                {
                    var soundSystem = EntitySystem.Get<AudioSystem>();
                    soundSystem.PlayAtCoords(_soundAutoEject, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }

                _magazineContainer.Remove(magazine);
                SendNetworkMessage(new MagazineAutoEjectMessage());
            }
            return true;
        }

        public void RemoveMagazine(IEntity user)
        {
            var mag = _magazineContainer.ContainedEntity;

            if (mag == null)
            {
                return;
            }

            if (MagNeedsOpenBolt && !BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("Bolt needs to be open"));
                return;
            }

            _magazineContainer.Remove(mag);
            if (_soundMagEject != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundMagEject, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
            }

            if (user.TryGetComponent(out HandsComponent handsComponent))
            {
                handsComponent.PutInHandOrDrop(mag.GetComponent<ItemComponent>());
            }

            Dirty();
            UpdateAppearance();
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // Insert magazine
            if (eventArgs.Using.TryGetComponent(out RangedMagazineComponent magazineComponent))
            {
                if ((MagazineTypes & magazineComponent.MagazineType) == 0)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong magazine type"));
                    return false;
                }

                if (magazineComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                    return false;
                }

                if (_magNeedsOpenBolt && !BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Need to open bolt first"));
                    return false;
                }

                if (_magazineContainer.ContainedEntity == null)
                {
                    if (_soundMagInsert != null)
                    {
                        EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundMagInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Magazine inserted"));
                    _magazineContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Already holding a magazine"));
                return false;
            }

            // Insert 1 ammo
            if (eventArgs.Using.TryGetComponent(out AmmoComponent ammoComponent))
            {
                if (!BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Cannot insert ammo while bolt is closed"));
                    return false;
                }

                if (ammoComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                    return false;
                }

                if (_chamberContainer.ContainedEntity == null)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("Ammo inserted"));
                    _chamberContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Chamber full"));
                return false;
            }

            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup(Loc.GetString("\nIt uses [color=white]{0}[/color] ammo.", Caliber));

            foreach (var magazineType in GetMagazineTypes())
            {
                message.AddMarkup(Loc.GetString("\nIt accepts [color=white]{0}[/color] magazines.", magazineType));
            }
        }

        [Verb]
        private sealed class EjectMagazineVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject magazine");
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                if (component.MagNeedsOpenBolt)
                {
                    data.Visibility = component.HasMagazine && component.BoltOpen
                        ? VerbVisibility.Visible
                        : VerbVisibility.Disabled;
                    return;
                }

                data.Visibility = component.HasMagazine ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.RemoveMagazine(user);
            }
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Open bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.BoltOpen = true;
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<ServerMagazineBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Close bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, ServerMagazineBarrelComponent component)
            {
                component.BoltOpen = false;
            }
        }
    }

    [Flags]
    public enum MagazineType
    {
        Unspecified = 0,
        LPistol = 1 << 0, // Placeholder?
        Pistol = 1 << 1,
        HCPistol = 1 << 2,
        Smg = 1 << 3,
        SmgTopMounted = 1 << 4,
        Rifle = 1 << 5,
        IH = 1 << 6, // Placeholder?
        Box = 1 << 7,
        Pan = 1 << 8,
        Dart = 1 << 9, // Placeholder
        CalicoTopMounted = 1 << 10,
    }
}
