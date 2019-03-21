using Content.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server.GameObjects
{
    public class Human : DamageTemplates
    {
        int critvalue = 200;
        int normalstates = 6;
        //string startsprite = "human0";

        public override List<(DamageType, int, ThresholdType)> AllowedStates => new List<(DamageType, int, ThresholdType)>()
        {
            (DamageType.Total, critvalue-1, ThresholdType.None),
            (DamageType.Total, critvalue, ThresholdType.Critical),
            (DamageType.Total, 300, ThresholdType.Death),
        };

        public override List<DamageThreshold> HealthHudThresholds
        {
            get
            {
                List<DamageThreshold> thresholds = new List<DamageThreshold>();
                thresholds.Add(new DamageThreshold(DamageType.Total, 1, ThresholdType.HUDUpdate));
                for (var i = 1; i <= normalstates; i++)
                {
                    thresholds.Add(new DamageThreshold(DamageType.Total, i * critvalue / normalstates, ThresholdType.HUDUpdate));
                }
                return thresholds; //we don't need to respecify the state damage thresholds since we'll update hud on damage state changes as well
            }
        }

        public override HudStateChange ChangeHudState(DamageableComponent damage)
        {
            ThresholdType healthstate = CalculateDamageState(damage);
            switch (healthstate)
            {
                case ThresholdType.None:
                    var totaldamage = damage.CurrentDamage[DamageType.Total];
                    if (totaldamage > critvalue)
                    {
                        throw new System.InvalidOperationException(); //these should all be below the crit value, possibly going over multiple thresholds at once?
                    }
                    var modifier = totaldamage / (critvalue / normalstates); //integer division floors towards zero
                    return new HudStateChange()
                    {
                        StateSprite = "Mob/UI/Human/human" + modifier.ToString() + ".png",
                        effect = ScreenEffects.None
                    };
                case ThresholdType.Critical:
                    return new HudStateChange()
                    {
                        StateSprite = "Mob/UI/Human/humancrit-0.png", //TODO: display as gif or alternate with -0 and -1 as frames
                        effect = ScreenEffects.GradientCircleMask
                    };
                case ThresholdType.Death:
                    return new HudStateChange()
                    {
                        StateSprite = "Mob/UI/Human/humandead.png",
                        effect = ScreenEffects.CircleMask
                    };
                default:
                    throw new System.InvalidOperationException();
            }
        }
    }
}
