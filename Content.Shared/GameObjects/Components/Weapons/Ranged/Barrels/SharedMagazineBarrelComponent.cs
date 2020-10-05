#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public abstract class SharedMagazineBarrelComponent : SharedRangedWeaponComponent
    {
        public override string Name => "MagazineBarrel";

        public override uint? NetID => ContentNetIDs.MAGAZINE_BARREL;
        
        [ViewVariables] public MagazineType MagazineTypes { get; private set; }
        
        [ViewVariables] public BallisticCaliber Caliber { get; private set; }

        public int Capacity { get; set; }

        public string? MagFillPrototype { get; private set; }

        public bool BoltOpen { get; protected set; }

        protected bool AutoEjectMag;
        // If the bolt needs to be open before we can insert / remove the mag (i.e. for LMGs)
        public bool MagNeedsOpenBolt { get; private set; }

        // Sounds
        public string? SoundBoltOpen { get; private set; }
        public string? SoundBoltClosed { get; private set; }
        public string? SoundRack { get; private set; }
        public string? SoundMagInsert { get; private set; }
        public string? SoundMagEject { get; private set; }
        public string? SoundAutoEject { get; private set; }

        protected const float AutoEjectVariation = 0.1f;
        protected const float MagVariation = 0.1f;
        protected const float RackVariation = 0.1f;

        protected const float AutoEjectVolume = 0.0f;
        protected const float MagVolume = 0.0f;
        protected const float RackVolume = 0.0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "magazineTypes",
                new List<MagazineType>(),
                types => types.ForEach(mag => MagazineTypes |= mag), GetMagazineTypes);

            serializer.DataReadWriteFunction("magNeedsOpenBolt", false, value => MagNeedsOpenBolt = value,
                () => MagNeedsOpenBolt);
            
            serializer.DataReadWriteFunction("caliber", BallisticCaliber.Unspecified, value => Caliber = value, () => Caliber);
            serializer.DataReadWriteFunction("magFillPrototype", null, value => MagFillPrototype = value, () => MagFillPrototype);
            serializer.DataField(ref AutoEjectMag, "autoEjectMag", false);

            serializer.DataReadWriteFunction("soundBoltOpen", null, value => SoundBoltOpen = value, () => SoundBoltOpen);
            serializer.DataReadWriteFunction("soundBoltClosed", null, value => SoundBoltClosed = value, () => SoundBoltClosed);
            serializer.DataReadWriteFunction("soundRack", null, value => SoundRack = value, () => SoundRack);
            serializer.DataReadWriteFunction("soundMagInsert", null, value => SoundMagInsert = value, () => SoundMagInsert);
            serializer.DataReadWriteFunction("soundMagEject", null, value => SoundMagEject = value, () => SoundMagEject);
            serializer.DataReadWriteFunction("soundAutoEject", "/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg", value => SoundAutoEject = value, () => SoundAutoEject);
        }

        protected List<MagazineType> GetMagazineTypes()
        {
            var types = new List<MagazineType>();

            foreach (var mag in (MagazineType[]) Enum.GetValues(typeof(MagazineType)))
            {
                if ((MagazineTypes & mag) != 0)
                {
                    types.Add(mag);
                }
            }

            return types;
        }
        
        protected abstract bool TrySetBolt(bool value);

        protected abstract void Cycle(bool manual = false);

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return UseEntity(eventArgs.User);
        }

        protected abstract bool UseEntity(IEntity user);

        protected abstract void TryEjectChamber();

        protected abstract void TryFeedChamber();

        protected abstract void RemoveMagazine(IEntity user);

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;

            return !BoltOpen;
        }

        protected abstract bool TryInsertMag(IEntity user, IEntity mag);

        protected abstract bool TryInsertAmmo(IEntity user, IEntity ammo);

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (TryInsertMag(eventArgs.User, eventArgs.Using))
            {
                return true;
            }

            if (TryInsertAmmo(eventArgs.User, eventArgs.Using))
            {
                return true;
            }

            return false;
        }
    }
    
    [Serializable, NetSerializable]
    public sealed class RemoveMagazineComponentMessage : ComponentMessage
    {
        public RemoveMagazineComponentMessage()
        {
            Directed = true;
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
    
    [Serializable, NetSerializable]
    public enum AmmoVisuals
    {
        AmmoCount,
        AmmoMax,
        Spent,
    }

    [Serializable, NetSerializable]
    public enum MagazineBarrelVisuals
    {
        MagLoaded
    }

    [Serializable, NetSerializable]
    public enum BarrelBoltVisuals
    {
        BoltOpen,
    }

    [Serializable, NetSerializable]
    public class MagazineBarrelComponentState : ComponentState
    {
        public bool BoltOpen { get; }
        public bool? Chambered { get; }
        public FireRateSelector FireRateSelector { get; }
        public Stack<bool>? Magazine { get; }

        public MagazineBarrelComponentState(
            bool boltOpen,
            bool? chambered, 
            FireRateSelector fireRateSelector, 
            Stack<bool>? magazine) : 
            base(ContentNetIDs.MAGAZINE_BARREL)
        {
            BoltOpen = boltOpen;
            Chambered = chambered;
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
        }
    }
}