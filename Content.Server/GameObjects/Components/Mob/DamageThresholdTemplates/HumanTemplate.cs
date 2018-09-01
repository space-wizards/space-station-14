using Content.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server.GameObjects
{
    public class Human : DamageTemplates
    {
        int critvalue = 200;
        int normalstates = 6;

        public override List<DamageThreshold> DamageThresholds => new List<DamageThreshold>()
            {
                new DamageThreshold(DamageType.Total, 300, ThresholdTypes.Death),
                new DamageThreshold(DamageType.Total, 200, ThresholdTypes.Critical)
            };

        public override List<ThresholdTypes> AllowedStates => new List<ThresholdTypes>()
            {
                ThresholdTypes.None,
                ThresholdTypes.Critical,
                ThresholdTypes.Death
            };

        public override List<DamageThreshold> HealthHudThresholds
        {
            get
            {
                List<DamageThreshold> thresholds = new List<DamageThreshold>();
                thresholds.Add(new DamageThreshold(DamageType.Total, 1, ThresholdTypes.HUDUpdate));
                for (var i = 1; i <= normalstates; i++)
                {
                    thresholds.Add(new DamageThreshold(DamageType.Total, i * critvalue / normalstates, ThresholdTypes.HUDUpdate));
                }
                return thresholds; //we don't need to respecify the state damage thresholds since we'll update hud on damage state changes as well
            }
        }

        public override HudStateChange ChangeHudState(ThresholdTypes state, DamageableComponent damage)
        {
            switch (state)
            {
                case ThresholdTypes.HUDUpdate:
                    var totaldamage = damage.CurrentDamage[DamageType.Total];
                    if (totaldamage > critvalue)
                    {
                        throw new System.Exception(); //these should all be below the crit value, possibly going over multiple thresholds at once?
                    }
                    var modifier = totaldamage / (critvalue / normalstates); //integer division floors towards zero
                    return new HudStateChange()
                    {
                        StateSprite = "human" + modifier.ToString(),
                    };
                case ThresholdTypes.Critical:
                    return new HudStateChange()
                    {
                        StateSprite = "humancrit",
                    };
                case ThresholdTypes.Death:
                    return new HudStateChange()
                    {
                        StateSprite = "humandead"
                    };
                default:
                    throw new System.Exception();
            }
        }
    }
}
