#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public abstract class SharedPumpBarrelComponent : SharedRangedWeaponComponent
    {
        public override string Name => "PumpBarrel";
        public override uint? NetID => ContentNetIDs.PUMP_BARREL;

        /// <summary>
        ///     Excluding chamber
        /// </summary>
        public int Capacity { get; protected set; }

        // Even a point having a chamber? I guess it makes some of the below code cleaner

        [ViewVariables]
        protected BallisticCaliber Caliber;

        [ViewVariables]
        public string? FillPrototype;
        [ViewVariables]
        protected int UnspawnedCount;
        
        /// <summary>
        ///     Excluding chamber
        /// </summary>
        protected abstract int ShotsLeft { get; }

        protected bool ManualCycle;

        // Sounds
        public string? SoundRack { get; private set; }
        public string? SoundInsert { get; private set; }
        
        protected const float RackVariation = 0.1f;
        
        protected const float RackVolume = 0.0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("capacity", 1, value => Capacity = value, () => Capacity);
            serializer.DataField(ref Caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref FillPrototype, "fillPrototype", null);
            serializer.DataField(ref ManualCycle, "manualCycle", true);

            serializer.DataReadWriteFunction("soundRack", "/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg", value => SoundRack = value, () => SoundRack);
            serializer.DataReadWriteFunction("soundInsert", "/Audio/Weapons/Guns/MagIn/bullet_insert.ogg", value => SoundInsert = value, () => SoundInsert);
        }

        protected abstract void Cycle(bool manual = false);

        public abstract bool TryInsertBullet(IEntity user, IEntity ammo);

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Cycle(true);
            Dirty();
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }
    }
    
    [Serializable, NetSerializable]
    public class PumpBarrelComponentState : ComponentState
    {
        public bool? Chamber { get; }
        public FireRateSelector FireRateSelector { get; }
        
        public int Capacity { get; }
        
        public Stack<bool> Ammo { get; }

        public PumpBarrelComponentState(
            bool? chamber,
            FireRateSelector fireRateSelector,
            int capacity,
            Stack<bool> ammo) :
            base(ContentNetIDs.PUMP_BARREL)
        {
            Chamber = chamber;
            FireRateSelector = fireRateSelector;
            Capacity = capacity;
            Ammo = ammo;
        }
    }
}
