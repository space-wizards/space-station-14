using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class ServerRangedBarrelComponentData
    {
        [CustomYamlField("spreadRatio")]
        public float SpreadRatio = 1f;

        [CustomYamlField("minAngle")]
        public Angle MinAngle;

        [CustomYamlField("maxAngle")]
        public Angle MaxAngle;

        [CustomYamlField("fireRate")]
        public float FireRate = 2f;

        [CustomYamlField("angleIncrease")]
        public float AngleIncrease;

        [CustomYamlField("angleDecay")]
        public float AngleDecay;

        [CustomYamlField("allRateSelectors")]
        public FireRateSelector AllRateSelectors;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref FireRate, "fireRate", 2f);

            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            serializer.DataReadWriteFunction(
                "minAngle",
                0,
                angle => MinAngle = Angle.FromDegrees(angle / 2f),
                () => MinAngle.Degrees * 2);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            serializer.DataReadWriteFunction(
                "maxAngle",
                45,
                angle => MaxAngle = Angle.FromDegrees(angle / 2f),
                () => MaxAngle.Degrees * 2);

            serializer.DataReadWriteFunction(
                "angleIncrease",
                40 / FireRate,
                angle => AngleIncrease = angle * (float) Math.PI / 180f,
                () => MathF.Round(AngleIncrease / ((float) Math.PI / 180f), 2));

            serializer.DataReadWriteFunction(
                "angleDecay",
                20f,
                angle => AngleDecay = angle * (float) Math.PI / 180f,
                () => MathF.Round(AngleDecay / ((float) Math.PI / 180f), 2));

            serializer.DataField(ref SpreadRatio, "ammoSpreadRatio", 1.0f);

            serializer.DataReadWriteFunction(
                "allSelectors",
                new List<FireRateSelector>(),
                selectors => selectors.ForEach(selector => AllRateSelectors |= selector),
                () =>
                {
                    var types = new List<FireRateSelector>();

                    foreach (FireRateSelector selector in Enum.GetValues(typeof(FireRateSelector)))
                    {
                        if ((AllRateSelectors & selector) != 0)
                        {
                            types.Add(selector);
                        }
                    }

                    return types;
                });

            // For simplicity we'll enforce it this way; ammo determines max spread
            if (SpreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }
        }

    }
}
