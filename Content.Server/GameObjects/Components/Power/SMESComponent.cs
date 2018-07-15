using System;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    public class SMESComponent : Component
    {
        public override string Name => "SMES";

        PowerStorageComponent Storage;
        AppearanceComponent Appearance;

        int LastChargeLevel = 0;
        ChargeState LastChargeState;

        public override void Initialize()
        {
            base.Initialize();
            Storage = Owner.GetComponent<PowerStorageComponent>();
            Appearance = Owner.GetComponent<AppearanceComponent>();
        }

        public override void Update(float frameTime)
        {
            var newLevel = CalcChargeLevel();
            if (newLevel != LastChargeLevel)
            {
                LastChargeLevel = newLevel;
                Appearance.SetData(SMESVisuals.LastChargeLevel, newLevel);
            }

            var newState = Storage.GetChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                Appearance.SetData(SMESVisuals.LastChargeState, newState);
            }
        }

        int CalcChargeLevel()
        {
            return ContentHelpers.RoundToLevels(Storage.Charge, Storage.Capacity, 6);
        }
    }
}
