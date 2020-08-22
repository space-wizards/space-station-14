using System.Collections.Generic;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Defines the threshold values for each damage state for any kind of species
    /// </summary>
    public abstract class DamageTemplates
    {
        public abstract List<DamageThreshold> HealthHudThresholds { get; }

        /// <summary>
        /// Changes the hud state when a threshold is reached
        /// </summary>
        /// <param name="damage"></param>
        /// <returns></returns>
        public abstract void ChangeHudState(DamageableComponent damage);

        //public abstract ResistanceSet resistanceset { get; }

        /// <summary>
        /// Shows allowed states, ordered by priority, closest to last value to have threshold reached is preferred
        /// </summary>
        public abstract List<(DamageType, int, ThresholdType)> AllowedStates { get; }

        /// <summary>
        /// Map of ALL POSSIBLE damage states to the threshold enum value that will trigger them, normal state wont be triggered by this value but is a default that is fell back onto
        /// </summary>
        public static Dictionary<ThresholdType, IDamageState> StateThresholdMap = new Dictionary<ThresholdType, IDamageState>()
        {
            { ThresholdType.None, new NormalState() },
            { ThresholdType.Critical, new CriticalState() },
            { ThresholdType.Death, new DeadState() }
        };

        public List<DamageThreshold> DamageThresholds
        {
            get
            {
                List<DamageThreshold> thresholds = new List<DamageThreshold>();
                foreach (var element in AllowedStates)
                {
                    thresholds.Add(new DamageThreshold(element.Item1, element.Item2, element.Item3));
                }
                return thresholds;
            }
        }

        public ThresholdType CalculateDamageState(DamageableComponent damage)
        {
            ThresholdType healthstate = ThresholdType.None;
            foreach(var element in AllowedStates)
            {
                if(damage.CurrentDamage[element.Item1] >= element.Item2)
                {
                    healthstate = element.Item3;
                }
            }

            return healthstate;
        }
    }
}
