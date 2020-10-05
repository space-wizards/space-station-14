#nullable enable
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    [ComponentReference(typeof(SharedRevolverBarrelComponent))]
    public sealed class ServerRevolverBarrelComponent : SharedRevolverBarrelComponent
    {
        private IEntity?[] _ammoSlots = null!;

        private IContainer AmmoContainer { get; set; } = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref FillPrototype, "fillPrototype", null);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            IActorComponent? actorComponent = null;
            var user = Shooter();
            user?.TryGetComponent(out actorComponent);

            if (user == null || session != actorComponent?.playerSession)
            {
                // Cheater / lagger?
                return;
            }

            switch (message)
            {
                case RevolverSpinMessage msg:
                    CurrentSlot = msg.Slot;
                    if (SoundSpin != null)
                        EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundSpin, Owner, AudioHelpers.WithVariation(SpinVariation).WithVolume(SpinVolume), excludedSession: Shooter().PlayerSession());
                    
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            var bullets = new bool?[_ammoSlots.Length];
            var unspawned = UnspawnedCount;
            
            for (var i = 0; i < _ammoSlots.Length; i++)
            {
                bullets[i] = !_ammoSlots[i]?.GetComponent<SharedAmmoComponent>().Spent;
                var bullet = !_ammoSlots[i]?.GetComponent<SharedAmmoComponent>().Spent;
                if (bullet == null && unspawned > 0)
                {
                    unspawned--;
                    bullet = true;
                }

                bullets[i] = bullet;
            }
            
            return new RevolverBarrelComponentState(CurrentSlot, Selector, bullets, SoundGunshot);
        }

        public override void Initialize()
        {
            base.Initialize();

            _ammoSlots = new IEntity?[Capacity];
            AmmoContainer = ContainerManagerComponent.Ensure<Container>("weapon-ammo", Owner, out var existing);

            if (existing)
            {
                DebugTools.Assert(AmmoContainer.ContainedEntities.Count <= _ammoSlots.Length);
                
                foreach (var entity in AmmoContainer.ContainedEntities)
                {
                    _ammoSlots[CurrentSlot] = entity;
                    Cycle();
                    UnspawnedCount--;
                }
            }

            if (FillPrototype != null)
            {
                UnspawnedCount += _ammoSlots.Length;
            }
        }

        protected override bool TryShoot(Angle angle)
        {
            // Revolvers can keep cycling even if the ammo is invalid.
            if (!base.TryShoot(angle))
                return false;

            var currentAmmo = _ammoSlots[CurrentSlot];

            if (currentAmmo == null)
            {
                if (UnspawnedCount > 0)
                {
                    var entity = Owner.EntityManager.SpawnEntity(FillPrototype, Owner.Transform.MapPosition);
                    _ammoSlots[CurrentSlot] = entity;
                    AmmoContainer.Insert(entity);
                    UnspawnedCount--;
                    currentAmmo = entity;
                }
                else
                {
                    Cycle();
                    return false;
                }
            } 
            else
            {
                if (currentAmmo.GetComponent<AmmoComponent>().Spent)
                {
                    Cycle();
                    return false;
                }
            }

            Cycle();
            var shooter = Shooter();
            var ammoComp = currentAmmo.GetComponent<AmmoComponent>();
            var sound = ammoComp.Spent ? SoundEmpty : SoundGunshot;
            
            if (sound != null)
                EntitySystem.Get<AudioSystem>().PlayFromEntity(sound, Owner, AudioHelpers.WithVariation(GunshotVariation).WithVolume(GunshotVolume), excludedSession: shooter.PlayerSession());

            if (!ammoComp.Spent)
            {
                EntitySystem.Get<RangedWeaponSystem>().ShootAmmo(shooter, this, angle, ammoComp);
                ammoComp.Spent = true;
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle);
            }

            return true;
        }

        protected override void EjectAllSlots()
        {
            var gunSystem = EntitySystem.Get<SharedRangedWeaponSystem>();
            // How many times we can play the sound
            const byte soundCap = 3;
            byte soundCount = 0;
            
            for (var i = 0; i < Capacity; i++)
            {
                var entity = _ammoSlots[i];
                if (entity == null)
                {
                    if (UnspawnedCount > 0)
                    {
                        entity = Owner.EntityManager.SpawnEntity(FillPrototype, Owner.Transform.MapPosition);
                        UnspawnedCount--;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    AmmoContainer.Remove(entity);
                }
                
                // TODO: Set to user when prediction is in
                _ammoSlots[i] = null;

                gunSystem.EjectCasing(null, entity, soundCount < soundCap);
                soundCount++;
            }

            // May as well point back at the end?
            CurrentSlot = _ammoSlots.Length - 1;
            Dirty();
        }

        public override bool TryInsertBullet(IEntity user, SharedAmmoComponent ammoComponent)
        {
            if (!base.TryInsertBullet(user, ammoComponent))
                return false;

            // Functions like a stack
            // These are inserted in reverse order but then when fired Cycle will go through in order
            // The reason we don't just use an actual stack is because spin can select a random slot to point at
            for (var i = _ammoSlots.Length - 1; i >= 0; i--)
            {
                var slot = _ammoSlots[i];
                if (slot == null)
                {
                    CurrentSlot = (byte) i;
                    _ammoSlots[i] = ammoComponent.Owner;
                    AmmoContainer.Insert(ammoComponent.Owner);
                    NextFire = IoCManager.Resolve<IGameTiming>().CurTime;
                    if (SoundInsert != null)
                        EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundInsert, Owner, AudioHelpers.WithVariation(InsertVariation).WithVolume(InsertVolume));

                    // Won't need dirty once prediction is in, place we can pass in the actual user above.
                    Dirty();
                    return true;
                }
            }
            
            return false;
        }
    }
}