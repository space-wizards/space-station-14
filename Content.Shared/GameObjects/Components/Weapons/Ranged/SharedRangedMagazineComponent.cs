#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    public abstract class SharedRangedMagazineComponent : Component, IInteractUsing, IUse
    {
        public override string Name => "RangedMagazine";

        public override uint? NetID => ContentNetIDs.RANGED_MAGAZINE;

        public abstract int ShotsLeft { get; }
        
        [ViewVariables] public int Capacity { get; private set; }

        [ViewVariables] public MagazineType MagazineType { get; private set; }
        
        [ViewVariables] public BallisticCaliber Caliber { get; private set; }

        // If there's anything already in the magazine
        [ViewVariables] public string? FillPrototype { get; private set; }
        // By default the magazine won't spawn the entity until needed so we need to keep track of how many left we can spawn
        // Generally you probably don't want to use this
        protected int UnspawnedCount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("capacity", 20, value => Capacity = value, () => Capacity);
            serializer.DataReadWriteFunction("magazineType", MagazineType.Unspecified, value => MagazineType = value, () => MagazineType);
            serializer.DataReadWriteFunction("caliber", BallisticCaliber.Unspecified, value => Caliber = value, () => Caliber);
            serializer.DataReadWriteFunction("fillPrototype", null, value => FillPrototype = value, () => FillPrototype);
        }

        protected abstract bool TryInsertAmmo(IEntity user, IEntity ammo);

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertAmmo(eventArgs.User, eventArgs.Using);
        }

        protected abstract bool Use(IEntity user);

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return Use(eventArgs.User);
        }
    }

    [Serializable, NetSerializable]
    public sealed class RangedMagazineComponentState : ComponentState
    {
        public Stack<bool> SpawnedAmmo { get; }
        
        public RangedMagazineComponentState(Stack<bool> spawnedAmmo) : base(ContentNetIDs.RANGED_MAGAZINE)
        {
            SpawnedAmmo = spawnedAmmo;
        }
    }

    [Serializable, NetSerializable]
    public sealed class DumpRangedMagazineComponentMessage : ComponentMessage
    {
        public byte Amount { get; }

        public DumpRangedMagazineComponentMessage(byte amount)
        {
            Amount = amount;
            Directed = true;
        }
    }
}