using System.Collections.Generic;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Defines the threshold values for each damage state for any kind of species
    /// </summary>
    public abstract class DamageTemplates
    {
        public abstract List<DamageThreshold> DamageThresholds { get; }

        public abstract List<DamageThreshold> HealthHudThresholds { get; }

        /// <summary>
        /// Changes the hud state when a threshold is reached
        /// </summary>
        /// <param name="state"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public abstract HudStateChange ChangeHudState(ThresholdTypes state, DamageableComponent damage);

        //public abstract ResistanceSet resistanceset { get; }

        /// <summary>
        /// Shows allowed states, ordered by priority, closest to last value to have threshold reached is preferred
        /// </summary>
        public abstract List<ThresholdTypes> AllowedStates { get; }

        /// <summary>
        /// Map of damage states to the threshold enum value that will trigger them, normal state wont be triggered by this value but is a default that is fell back onto
        /// </summary>
        public static Dictionary<ThresholdTypes, DamageState> StateThresholdMap = new Dictionary<ThresholdTypes, DamageState>()
        {
            { ThresholdTypes.None, new NormalState() },
            { ThresholdTypes.Critical, new CriticalState() },
            { ThresholdTypes.Death, new DeadState() }
        };
    }
}
