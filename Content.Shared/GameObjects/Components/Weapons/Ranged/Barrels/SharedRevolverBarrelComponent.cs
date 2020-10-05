#nullable enable
using System;
using System.Threading.Tasks;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public abstract class SharedRevolverBarrelComponent : SharedRangedWeaponComponent
    {
        public override string Name => "RevolverBarrel";
        public override uint? NetID => ContentNetIDs.REVOLVER_BARREL;

        public BallisticCaliber Caliber;
        
        /// <summary>
        ///     What slot will be used for the next bullet.
        /// </summary>
        protected int CurrentSlot = 0;

        protected int Capacity { get; set; }

        public string? FillPrototype;
        
        /// <summary>
        ///     To avoid spawning entities in until necessary we'll just keep a counter for the unspawned default ammo.
        /// </summary>
        protected int UnspawnedCount;

        // Sounds
        public string? SoundEject { get; private set; }
        public string? SoundInsert { get; private set; }
        public string? SoundSpin { get; private set; }

        protected const float SpinVariation = 0.1f;

        protected const float SpinVolume = 0.0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "capacity",
                6,
                cap => Capacity = cap,
                () => Capacity);
            
            serializer.DataField(ref Caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref FillPrototype, "fillPrototype", null);

            serializer.DataReadWriteFunction("soundEject", "/Audio/Weapons/Guns/MagOut/revolver_magout.ogg", value => SoundEject = value, () => SoundEject);
            serializer.DataReadWriteFunction("soundInsert", "/Audio/Weapons/Guns/MagIn/revolver_magin.ogg", value => SoundInsert = value, () => SoundInsert);
            serializer.DataReadWriteFunction("soundSpin", "/Audio/Weapons/Guns/Misc/revolver_spin.ogg", value => SoundSpin = value, () => SoundSpin);
        }

        protected void Cycle()
        {
            // Move up a slot
            CurrentSlot = (CurrentSlot + 1) % Capacity;
        }

        /// <summary>
        ///     Dumps all cartridges onto the ground.
        /// </summary>
        /// <returns>The number of cartridges ejected</returns>
        protected abstract void EjectAllSlots();

        public virtual bool TryInsertBullet(IEntity user, SharedAmmoComponent ammoComponent)
        {
            if (ammoComponent.Caliber != Caliber)
                return false;

            return true;
        }
        
        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out SharedAmmoComponent? ammoComponent))
            {
                return false;
            }

            return TryInsertBullet(eventArgs.User, ammoComponent);
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            EjectAllSlots();
            return true;
        }
    }
    
    [Serializable, NetSerializable]
    public class RevolverBarrelComponentState : ComponentState
    {
        public int CurrentSlot { get; }
        public FireRateSelector FireRateSelector { get; }
        public bool?[] Bullets { get; }
        public string? SoundGunshot { get; }

        public RevolverBarrelComponentState(
            int currentSlot,
            FireRateSelector fireRateSelector,
            bool?[] bullets,
            string? soundGunshot) :
            base(ContentNetIDs.REVOLVER_BARREL)
        {
            CurrentSlot = currentSlot;
            FireRateSelector = fireRateSelector;
            Bullets = bullets;
            SoundGunshot = soundGunshot;
        }
    }
    
    [Serializable, NetSerializable]
    public class ChangeSlotMessage : ComponentMessage
    {
        public int Slot { get; }
        
        public ChangeSlotMessage(int slot)
        {
            Slot = slot;
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RevolverSpinMessage : ChangeSlotMessage
    {
        public RevolverSpinMessage(int slot) : base(slot)
        {
            
        }
    }
}