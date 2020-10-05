#nullable enable
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Player;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    [ComponentReference(typeof(SharedBoltActionBarrelComponent))]
    public class ServerBoltActionBarrelComponent : SharedBoltActionBarrelComponent
    {
        private ContainerSlot _chamberContainer = default!;
        private Container _ammoContainer = default!;
        private Stack<IEntity> _spawnedAmmo = new Stack<IEntity>();

        public override void Initialize()
        {
            base.Initialize();
            _chamberContainer = ContainerManagerComponent.Ensure<ContainerSlot>("bolt-chamber", Owner, out var existing);
            _ammoContainer = ContainerManagerComponent.Ensure<Container>("bolt-ammo", Owner, out existing);

            if (FillPrototype == null)
            {
                UnspawnedCount = 0;
            }
            else
            {
                UnspawnedCount += Capacity;
            }

            if (existing)
            {
                UnspawnedCount--;
            }
            else if (UnspawnedCount > 0)
            {
                var entity = Owner.EntityManager.SpawnEntity(FillPrototype, Owner.Transform.MapPosition);
                _chamberContainer?.Insert(entity);
                UnspawnedCount--;
            }
            
            if (existing && _ammoContainer != null)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    UnspawnedCount--;
                }
            }
            
            Dirty();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            if (session?.AttachedEntity != Shooter())
                return;

            switch (message)
            {
                case BoltChangedComponentMessage msg:
                    SetBolt(msg.BoltOpen);
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            var chamber = !_chamberContainer?.ContainedEntity?.GetComponent<SharedAmmoComponent>().Spent;
            var ammo = new Stack<bool?>();

            foreach (var entity in _spawnedAmmo)
            {
                ammo.Push(!entity.GetComponent<SharedAmmoComponent>().Spent);
            }

            for (var i = 0; i < UnspawnedCount; i++)
            {
                ammo.Push(true);
            }

            return new BoltActionBarrelComponentState(BoltOpen, chamber, Selector, ammo, SoundGunshot);
        }

        protected override void SetBolt(bool value)
        {
            if (BoltOpen == value)
                return;

            if (value)
            {
                TryEjectChamber();
                if (SoundBoltOpen != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundBoltOpen, Owner, AudioHelpers.WithVariation(BoltToggleVariation).WithVolume(BoltToggleVolume), excludedSession: Shooter().PlayerSession());
                }
            }
            else
            {
                TryFeedChamber();
                if (SoundBoltClosed != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundBoltClosed, Owner, AudioHelpers.WithVariation(BoltToggleVariation).WithVolume(BoltToggleVolume), excludedSession: Shooter().PlayerSession());
                }
            }

            BoltOpen = value;
        }

        protected override void Cycle(bool manual = false)
        {
            TryEjectChamber();
            TryFeedChamber();
            var shooter = Shooter();

            if (_chamberContainer?.ContainedEntity == null && manual)
            {
                SetBolt(true);
                if (shooter != null)
                    Owner.PopupMessage(shooter, Loc.GetString("Bolt opened"));
                
                return;
            }

            // TODO: Need this because interaction prediction
            if (SoundRack != null)
                EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundRack, Owner, AudioHelpers.WithVariation(CycleVariation).WithVolume(CycleVolume));

            Dirty();
        }
        
        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;
            
            var chamberEntity = _chamberContainer?.ContainedEntity;
            if (AutoCycle)
                Cycle();
            
            if (chamberEntity == null)
                return true;
            
            var shooter = Shooter();
            var ammoComp = chamberEntity.GetComponent<AmmoComponent>();
            var sound = ammoComp.Spent ? SoundEmpty : SoundGunshot;
            
            if (sound != null)
                EntitySystem.Get<AudioSystem>().PlayFromEntity(sound, Owner, AudioHelpers.WithVariation(GunshotVariation).WithVolume(GunshotVolume), excludedSession: shooter.PlayerSession());

            if (!ammoComp.Spent)
            {
                EntitySystem.Get<RangedWeaponSystem>().ShootAmmo(shooter, this, angle, ammoComp);
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle);
                ammoComp.Spent = true;
            }

            return true;
        }

        protected override void TryEjectChamber()
        {
            var chamber = _chamberContainer?.ContainedEntity;
            if (chamber == null)
                return;

            _chamberContainer?.Remove(chamber);
            EntitySystem.Get<SharedRangedWeaponSystem>().EjectCasing(Shooter(), chamber);
        }

        protected override void TryFeedChamber()
        {
            if (_spawnedAmmo.TryPop(out var ammo))
            {
                _chamberContainer?.Insert(ammo);
                if (SoundRack != null)
                    EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundRack, Owner, AudioHelpers.WithVariation(CycleVariation).WithVolume(CycleVolume), excludedSession: Shooter().PlayerSession());
                
                return;
            }

            if (UnspawnedCount > 0)
            {
                var entity = Owner.EntityManager.SpawnEntity(FillPrototype, Owner.Transform.MapPosition);
                _chamberContainer?.Insert(entity);
                if (SoundRack != null)
                    EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundRack, Owner, AudioHelpers.WithVariation(CycleVariation).WithVolume(CycleVolume), excludedSession: Shooter().PlayerSession());
                
                UnspawnedCount--;
            }
        }

        public override bool TryInsertBullet(IEntity user, SharedAmmoComponent ammoComponent)
        {
            if (ammoComponent.Caliber != Caliber)
                return false;

            if (_ammoContainer?.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer?.Insert(ammoComponent.Owner);
                _spawnedAmmo.Push(ammoComponent.Owner);

                if (SoundInsert != null)
                    EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundInsert, Owner, AudioHelpers.WithVariation(InsertVariation).WithVolume(InsertVolume));

                // TODO: when interaction predictions are in remove this.
                Dirty();
                return true;
            }

            return false;
        }
    }
}