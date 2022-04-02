using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed class DragonComponent : Component
    {
        /// <summary>
        /// Defines the devour action
        /// </summary>
        [DataField("devourAction", required: true)]
        public EntityTargetAction DevourAction = default!;

        // The amount of time per health point it takes to devour something.
        // For walls, it takes the damage tirgger value.
        [DataField("devourEffectiveness")]
        public float DevourMultiplier = default!;

        //The amount of eggs the dragon is ready to hatch
        public int EggsLeft = default!;
    }

    public sealed class DevourActionEvent : PerformEntityTargetActionEvent
    {
        [DataField("devourAction")]
        public EntityTargetAction DevourAction = new();  
    }


}
