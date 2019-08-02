using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Handles the "user-facing" side of the actual SMES object.
    ///     This is operations that are specific to the SMES, like UI and visuals.
    ///     Code interfacing with the powernet is handled in <see cref="PowerStorageComponent" />.
    /// </summary>
    [RegisterComponent]
    public class SmesComponent : Component
    {
        public override string Name => "Smes";

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

        public void OnUpdate()
        {
            var newLevel = CalcChargeLevel();
            if (newLevel != LastChargeLevel)
            {
                LastChargeLevel = newLevel;
                Appearance.SetData(SmesVisuals.LastChargeLevel, newLevel);
            }

            var newState = Storage.GetChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                Appearance.SetData(SmesVisuals.LastChargeState, newState);
            }
        }

        int CalcChargeLevel()
        {
            return ContentHelpers.RoundToLevels(Storage.Charge, Storage.Capacity, 6);
        }
    }
}
