using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ServerMagazineBarrelComponent : ServerRangedBarrelComponent, IExamine
    {
        public override string Name => "MagazineBarrel";

        [ViewVariables]
        private ContainerSlot _chamberContainer = default!;
        [ViewVariables] public bool HasMagazine => _magazineContainer.ContainedEntity != null;
        private ContainerSlot _magazineContainer = default!;

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
        private string? _magFillPrototype;

        public bool BoltOpen
        {
            get => _boltOpen;
            set
            {
                if (_boltOpen == value)
                {
                    return;
                }

                if (value)
                {
                    TryEjectChamber();
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }
                else
                {
                    TryFeedChamber();
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
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

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundBoltOpen", required: true)]
        private SoundSpecifier _soundBoltOpen = default!;
        [DataField("soundBoltClosed", required: true)]
        private SoundSpecifier _soundBoltClosed = default!;
        [DataField("soundRack", required: true)]
        private SoundSpecifier _soundRack = default!;
        [DataField("soundMagInsert", required: true)]
        private SoundSpecifier _soundMagInsert = default!;
        [DataField("soundMagEject", required: true)]
        private SoundSpecifier _soundMagEject = default!;
        [DataField("soundAutoEject")]
        private SoundSpecifier _soundAutoEject = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

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
            if (magazine != null && magazine.TryGetComponent(out RangedMagazineComponent? rangedMagazineComponent))
            {
                count = (rangedMagazineComponent.ShotsLeft, rangedMagazineComponent.Capacity);
            }

            return new MagazineBarrelComponentState(
                _chamberContainer.ContainedEntity != null,
                FireRateSelector,
                count,
                SoundGunshot.GetSound());
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
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
            base.Startup();
            UpdateAppearance();
        }

        public override IEntity? PeekAmmo()
        {
            return BoltOpen ? null : _chamberContainer.ContainedEntity;
        }

        public override IEntity? TakeProjectile(EntityCoordinates spawnAt)
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

            if (_chamberContainer.ContainedEntity == null && !BoltOpen)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-5));

                if (Owner.TryGetContainer(out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("server-magazine-barrel-component-cycle-bolt-open"));
                }
                BoltOpen = true;
                return;
            }

            if (manual)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundRack.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
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
                SoundSystem.Play(Filter.Pvs(Owner), _soundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-5));
                Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-use-entity-bolt-closed"));
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
                SoundSystem.Play(Filter.Pvs(Owner), _soundAutoEject.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

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
                Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-remove-magazine-bolt-closed"));
                return;
            }

            _magazineContainer.Remove(mag);
            SoundSystem.Play(Filter.Pvs(Owner), _soundMagEject.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

            if (user.TryGetComponent(out HandsComponent? handsComponent))
            {
                handsComponent.PutInHandOrDrop(mag.GetComponent<ItemComponent>());
            }

            Dirty();
            UpdateAppearance();
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // Insert magazine
            if (eventArgs.Using.TryGetComponent(out RangedMagazineComponent? magazineComponent))
            {
                if ((MagazineTypes & magazineComponent.MagazineType) == 0)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-magazine-type"));
                    return false;
                }

                if (magazineComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"));
                    return false;
                }

                if (_magNeedsOpenBolt && !BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-bolt-closed"));
                    return false;
                }

                if (_magazineContainer.ContainedEntity == null)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _soundMagInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-success"));
                    _magazineContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-already-holding-magazine"));
                return false;
            }

            // Insert 1 ammo
            if (eventArgs.Using.TryGetComponent(out AmmoComponent? ammoComponent))
            {
                if (!BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-bolt-closed"));
                    return false;
                }

                if (ammoComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"));
                    return false;
                }

                if (_chamberContainer.ContainedEntity == null)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-success"));
                    _chamberContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-full"));
                return false;
            }

            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup("\n" + Loc.GetString("server-magazine-barrel-component-on-examine", ("caliber", Caliber)));

            foreach (var magazineType in GetMagazineTypes())
            {
                message.AddMarkup("\n" + Loc.GetString("server-magazine-barrel-component-on-examine-magazine-type", ("magazineType", magazineType)));
            }
        }

        [Verb]
        private sealed class EjectMagazineVerb : Verb<ServerMagazineBarrelComponent>
        {
            public override bool AlternativeInteraction => true;

            protected override void GetData(IEntity user, ServerMagazineBarrelComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("eject-magazine-verb-get-data-text");
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
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("open-bolt-verb-get-data-text");
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
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("close-bolt-verb-get-data-text");
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
