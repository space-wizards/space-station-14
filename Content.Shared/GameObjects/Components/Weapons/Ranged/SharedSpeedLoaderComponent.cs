#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    public abstract class SharedSpeedLoaderComponent : Component, IInteractUsing, IUse, IAfterInteract
    {
        public override string Name => "SpeedLoader";
        public override uint? NetID => ContentNetIDs.SPEED_LOADER;

        [ViewVariables] protected BallisticCaliber Caliber { get; private set; }
        
        [ViewVariables] public int Capacity { get; protected set; }

        protected int UnspawnedCount;

        public abstract int ShotsLeft { get; }

        public string? FillPrototype { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("caliber", BallisticCaliber.Unspecified, value => Caliber = value, () => Caliber);
            serializer.DataReadWriteFunction("capacity", 6, value => Capacity = value, () => Capacity);
            serializer.DataReadWriteFunction("fillPrototype", null, value => FillPrototype = value, () => FillPrototype);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (FillPrototype != null)
            {
                UnspawnedCount += Capacity;
            }
            else
            {
                UnspawnedCount = 0;
            }
        }

        public virtual bool TryInsertAmmo(IEntity user, SharedAmmoComponent ammoComponent)
        {
            if (ammoComponent.Caliber != Caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;   
            }

            return true;
        }

        protected abstract bool UseEntity(IEntity user);

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            AfterInteract(eventArgs);
        }

        protected abstract void AfterInteract(AfterInteractEventArgs eventArgs);

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out SharedAmmoComponent? ammoComponent))
            {
                return TryInsertAmmo(eventArgs.User, ammoComponent);
            }

            return false;
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return UseEntity(eventArgs.User);
        }
    }
    
    [Serializable, NetSerializable]
    public sealed class SpeedLoaderComponentState : ComponentState
    {
        public int Capacity { get; }
        
        public Stack<bool> Ammo { get; }
        
        public SpeedLoaderComponentState(int capacity, Stack<bool> ammo) : base(ContentNetIDs.SPEED_LOADER)
        {
            Capacity = capacity;
            Ammo = ammo;
        }
    }
}