#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public abstract class SharedBatteryBarrelComponent : SharedRangedWeaponComponent
    {
        public override string Name => "BatteryBarrel";
        public override uint? NetID => ContentNetIDs.BATTERY_BARREL;
        
        /// <summary>
        ///     The minimum change we need before we can fire
        /// </summary>
        [ViewVariables] protected float LowerChargeLimit;
        
        /// <summary>
        ///     How much energy it costs to fire a full shot.
        ///     We can also fire partial shots if LowerChargeLimit is met.
        /// </summary>
        [ViewVariables] protected float BaseFireCost;
        
        // What gets fired
        [ViewVariables] public string AmmoPrototype { get; private set; } = default!;
        
        // Could use an interface instead but eh, if there's more than hitscan / projectiles in the future you can change it.
        protected bool AmmoIsHitscan;

        // Sounds
        public string? SoundPowerCellInsert { get; private set; }
        public string? SoundPowerCellEject { get; private set; }
        
        // Audio profile
        protected const float CellInsertVariation = 0.1f;
        protected const float CellEjectVariation = 0.1f;

        protected const float CellInsertVolume = 0.0f;
        protected const float CellEjectVolume = 0.0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("ammoPrototype", string.Empty, value => AmmoPrototype = value, () => AmmoPrototype);
            serializer.DataField(ref LowerChargeLimit, "lowerChargeLimit", 10);
            serializer.DataField(ref BaseFireCost, "baseFireCost", 300.0f);
            
            serializer.DataReadWriteFunction("soundPowerCellInsert", null, value => SoundPowerCellInsert = value, () => SoundPowerCellInsert);
            serializer.DataReadWriteFunction("soundPowerCellEject", null, value => SoundPowerCellEject = value, () => SoundPowerCellEject);
        }

        public override void Initialize()
        {
            base.Initialize();
            AmmoIsHitscan = IoCManager.Resolve<IPrototypeManager>().HasIndex<HitscanPrototype>(AmmoPrototype);
        }

        public abstract void UpdateAppearance();
    }
    
    [Serializable, NetSerializable]
    public sealed class BatteryBarrelComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }
        public (float current, float max)? PowerCell { get; }

        public BatteryBarrelComponentState(
            FireRateSelector fireRateSelector,
            (float current, float max)? powerCell) :
            base(ContentNetIDs.BATTERY_BARREL)
        {
            FireRateSelector = fireRateSelector;
            PowerCell = powerCell;
        }
    }
}
