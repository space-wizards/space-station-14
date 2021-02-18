using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class ServerRangedBarrelComponentData : ISerializationHooks
    {
        [DataField("ammoSpreadRatio")]
        [DataClassTarget("spreadRatio")]
        public float SpreadRatio = 1f;

        [DataField("minAngle")]
        public float MinAngleDegrees;

        [DataClassTarget("minAngle")]
        public Angle MinAngle;

        [DataField("maxAngle")]
        public float MaxAngleDegrees = 45;

        [DataClassTarget("maxAngle")]
        public Angle MaxAngle;

        [DataField("fireRate")]
        [DataClassTarget("fireRate")]
        public float FireRate = 2f;

        [DataField("angleIncrease")]
        public float? AngleIncreaseDegrees;

        [DataClassTarget("angleIncrease")]
        public float AngleIncrease;

        [DataField("angleDecay")]
        public float AngleDecayDegrees = 20;

        [DataClassTarget("angleDecay")]
        public float AngleDecay;

        [DataField("allRateSelectors")]
        [DataClassTarget("allRateSelectors")]
        public FireRateSelector AllRateSelectors;

        public void BeforeSerialization()
        {
            MinAngleDegrees = (float) (MinAngle.Degrees * 2);
            MaxAngleDegrees = (float) (MaxAngle.Degrees * 2);
            AngleIncreaseDegrees = MathF.Round(AngleIncrease / ((float) Math.PI / 180f), 2);
            AngleDecay = MathF.Round(AngleDecay / ((float) Math.PI / 180f), 2);
        }

        public void AfterDeserialization()
        {
            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            MinAngle = Angle.FromDegrees(MinAngleDegrees / 2f);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            MaxAngle = Angle.FromDegrees(MaxAngleDegrees / 2f);

            AngleIncreaseDegrees ??= 40 / FireRate;
            AngleIncrease = AngleIncreaseDegrees.Value * (float) Math.PI / 180f;

            AngleDecay = AngleDecayDegrees * (float) Math.PI / 180f;

            // For simplicity we'll enforce it this way; ammo determines max spread
            if (SpreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }
        }
    }
}
