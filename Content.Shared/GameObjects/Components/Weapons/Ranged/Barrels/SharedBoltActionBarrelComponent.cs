#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public abstract class SharedBoltActionBarrelComponent : SharedRangedWeaponComponent
    {
        // Originally I had this logic shared with PumpBarrel and used a couple of variables to control things
        // but it felt a lot messier to play around with, especially when adding verbs

        public override string Name => "BoltActionBarrel";
        public override uint? NetID => ContentNetIDs.BOLTACTION_BARREL;

        [ViewVariables] protected int Capacity { get; private set; }

        [ViewVariables]
        public BallisticCaliber Caliber;

        [ViewVariables]
        public string? FillPrototype;
        [ViewVariables]
        protected int UnspawnedCount;

        public bool BoltOpen { get; protected set; }
        protected bool AutoCycle;

        // Sounds
        public string? SoundRack { get; private set; }
        public string? SoundBoltOpen { get; private set; }
        public string? SoundBoltClosed { get; private set; }
        public string? SoundInsert { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("caliber", BallisticCaliber.Unspecified, value => Caliber = value, () => Caliber);
            serializer.DataReadWriteFunction("capacity", Capacity, value => Capacity = value, () => Capacity);
            serializer.DataReadWriteFunction("fillPrototype", null, value => FillPrototype = value, () => FillPrototype);
            serializer.DataReadWriteFunction("autoCycle", false, value => AutoCycle = value, () => AutoCycle);

            serializer.DataReadWriteFunction("soundRack", "/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg", value => SoundRack = value, () => SoundRack);
            serializer.DataReadWriteFunction("soundBoltOpen", "/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg", value => SoundBoltOpen = value, () => SoundBoltOpen);
            serializer.DataReadWriteFunction("soundBoltClosed", "/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg", value => SoundBoltClosed = value, () => SoundBoltClosed);
            serializer.DataReadWriteFunction("soundInsert", "/Audio/Weapons/Guns/MagIn/bullet_insert.ogg", value => SoundInsert = value, () => SoundInsert);
        }

        protected abstract void SetBolt(bool value);

        protected abstract void TryEjectChamber();

        protected abstract void TryFeedChamber();

        protected abstract void Cycle(bool manual = false);

        public abstract bool TryInsertBullet(IEntity user, SharedAmmoComponent ammoComponent);

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;

            return !BoltOpen;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out SharedAmmoComponent? ammoComponent))
                return false;
            
            return TryInsertBullet(eventArgs.User, ammoComponent);
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (BoltOpen)
            {
                Dirty();
                SetBolt(false);
                // TODO: Predict when they're in plx
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Bolt closed"));
                return true;
            }

            Cycle(true);
            Dirty();
            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BoltActionBarrelComponentState : ComponentState
    {
        public bool BoltOpen { get; }
        public bool? Chamber { get; }
        public FireRateSelector FireRateSelector { get; }
        public Stack<bool?> Bullets { get; }
        public string? SoundGunshot { get; }

        public BoltActionBarrelComponentState(
            bool boltOpen,
            bool? chamber,
            FireRateSelector fireRateSelector,
            Stack<bool?> bullets,
            string? soundGunshot) :
            base(ContentNetIDs.BOLTACTION_BARREL)
        {
            BoltOpen = boltOpen;
            Chamber = chamber;
            FireRateSelector = fireRateSelector;
            Bullets = bullets;
            SoundGunshot = soundGunshot;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BoltChangedComponentMessage : ComponentMessage
    {
        public bool BoltOpen { get; }

        public BoltChangedComponentMessage(bool boltOpen)
        {
            BoltOpen = boltOpen;
            Directed = true;
        }
    }
}
