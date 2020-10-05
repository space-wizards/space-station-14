#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Localization;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedMagazineComponent))]
    public class ServerRangedMagazineComponent : SharedRangedMagazineComponent
    {
        private Container _ammoContainer = default!;

        public IReadOnlyCollection<IEntity> SpawnedAmmo => _spawnedAmmo;
        private Stack<IEntity> _spawnedAmmo = new Stack<IEntity>();
        
        public override int ShotsLeft => _spawnedAmmo.Count + UnspawnedCount;

        public override void Initialize()
        {
            base.Initialize();
            
            _ammoContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-magazine", Owner, out var existing);

            if (FillPrototype != null)
            {
                UnspawnedCount += Capacity;
            }
            else
            {
                UnspawnedCount = 0;
            }

            if (existing)
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
            
            // If it's not on the ground / in our inventory then block it
            if (ContainerHelpers.TryGetContainer(Owner, out var container) && container.Owner != session?.AttachedEntity)
                return;

            switch (message)
            {
                case DumpRangedMagazineComponentMessage msg:
                    Dump(session?.AttachedEntity, msg.Amount);
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            var ammo = new Stack<bool>();

            foreach (var entity in _spawnedAmmo)
            {
                ammo.Push(!entity.GetComponent<SharedAmmoComponent>().Spent);
            }
            
            for (var i = 0; i < UnspawnedCount; i++)
            {
                ammo.Push(true);
            }
                
            return new RangedMagazineComponentState(ammo);
        }

        public void Dump(IEntity? user, int amount)
        {
            var count = Math.Min(amount, ShotsLeft);
            const byte maxSounds = 3;
            var soundsPlayed = 0;

            for (var i = 0; i < count; i++)
            {
                if (!TryPop(out var entity))
                    break;
                
                EntitySystem.Get<SharedRangedWeaponSystem>().EjectCasing(user, entity, soundsPlayed < maxSounds);
                soundsPlayed++;
            }
        }

        public bool TryPop([NotNullWhen(true)] out IEntity? entity)
        {
            if (_spawnedAmmo.TryPop(out entity))
            {
                Dirty();
                return true;
            }

            if (UnspawnedCount > 0)
            {
                entity = Owner.EntityManager.SpawnEntity(FillPrototype, Owner.Transform.Coordinates);
                UnspawnedCount--;
                Dirty();
                return true;
            }

            return false;
        }

        protected override bool TryInsertAmmo(IEntity user, IEntity ammo)
        {
            // TODO: Move popups to client-side when possible
            if (!ammo.TryGetComponent(out SharedAmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != Caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (ShotsLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("Magazine is full"));
                return false;
            }

            _ammoContainer.Insert(ammo);
            _spawnedAmmo.Push(ammo);
            Dirty();
            return true;
        }

        protected override bool Use(IEntity user)
        {
            if (!user.TryGetComponent(out HandsComponent? handsComponent))
                return false;

            if (!TryPop(out var ammo))
                return false;

            var itemComponent = ammo.GetComponent<ItemComponent>();
            if (!handsComponent.CanPutInHand(itemComponent))
            {
                EntitySystem.Get<SharedRangedWeaponSystem>().EjectCasing(user, ammo);
            }
            else
            {
                handsComponent.PutInHand(itemComponent);
            }

            return true;
        }
    }
}