using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;

namespace Content.Server.GameObjects
{
    [UsedImplicitly]
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

        public override void ChangeHudState(DamageableComponent damage)
        {
            ThresholdType healthstate = CalculateDamageState(damage);
            damage.Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
            damage.Owner.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent);
            switch (healthstate)
            {
                case ThresholdType.None:
                    var totaldamage = damage.CurrentDamage[DamageType.Total];
                    if (totaldamage > critvalue)
                    {
                        throw new InvalidOperationException(); //these should all be below the crit value, possibly going over multiple thresholds at once?
                    }
                    var modifier = totaldamage / (critvalue / normalstates); //integer division floors towards zero
                    statusEffectsComponent?.ChangeStatus(StatusEffect.Health,
                            "/Textures/Mob/UI/Human/human" + modifier + ".png");

                    overlayComponent?.ChangeOverlay(ScreenEffects.None);

                    return;
                case ThresholdType.Critical:
                    statusEffectsComponent?.ChangeStatus(
                        StatusEffect.Health,
                        "/Textures/Mob/UI/Human/humancrit-0.png");
                    overlayComponent?.ChangeOverlay(ScreenEffects.GradientCircleMask);

                    return;
                case ThresholdType.Death:
                    statusEffectsComponent?.ChangeStatus(
                        StatusEffect.Health,
                        "/Textures/Mob/UI/Human/humandead.png");
                    overlayComponent?.ChangeOverlay(ScreenEffects.CircleMask);

                    return;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
