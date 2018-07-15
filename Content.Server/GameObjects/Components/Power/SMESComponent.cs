using System;
using Content.Shared.Utility;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    public class SMESComponent : Component
    {
        public override string Name => "SMES";

        const int SPRITE_LAYER_INPUT = 2;
        const int SPRITE_LAYER_CHARGE = 3;
        const int SPRITE_LAYER_OUTPUT = 4;

        PowerStorageComponent Storage;
        SpriteComponent Sprite;

        int LastChargeLevel = 0;
        PowerStorageComponent.ChargeState LastChargeState;

        public override void Initialize()
        {
            base.Initialize();
            Storage = Owner.GetComponent<PowerStorageComponent>();
            Sprite = Owner.GetComponent<SpriteComponent>();
        }

        public override void Update(float frameTime)
        {
            bool doUpdate = false;
            var newLevel = CalcChargeLevel();
            if (newLevel != LastChargeLevel)
            {
                LastChargeLevel = newLevel;
                doUpdate = true;
            }

            var newState = Storage.GetChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                doUpdate = true;
            }

            if (doUpdate)
            {
                UpdateIcon();
            }
        }

        int CalcChargeLevel()
        {
            return ContentHelpers.RoundToLevels(Storage.Charge, Storage.Capacity, 6);
        }

        void UpdateIcon()
        {
            if (LastChargeLevel == 0)
            {
                Sprite.LayerSetVisible(SPRITE_LAYER_CHARGE, false);
            }
            else
            {
                Sprite.LayerSetVisible(SPRITE_LAYER_CHARGE, true);
                Sprite.LayerSetState(SPRITE_LAYER_CHARGE, $"smes-og{LastChargeLevel}");
            }

            switch (LastChargeState)
            {
                case PowerStorageComponent.ChargeState.Still:
                    Sprite.LayerSetState(SPRITE_LAYER_INPUT, "smes-oc0");
                    Sprite.LayerSetState(SPRITE_LAYER_OUTPUT, "smes-op1");
                    break;
                case PowerStorageComponent.ChargeState.Charging:
                    Sprite.LayerSetState(SPRITE_LAYER_INPUT, "smes-oc1");
                    Sprite.LayerSetState(SPRITE_LAYER_OUTPUT, "smes-op1");
                    break;
                case PowerStorageComponent.ChargeState.Discharging:
                    Sprite.LayerSetState(SPRITE_LAYER_INPUT, "smes-oc0");
                    Sprite.LayerSetState(SPRITE_LAYER_OUTPUT, "smes-op2");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
