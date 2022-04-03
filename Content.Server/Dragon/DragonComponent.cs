using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using System.Threading;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed class DragonComponent : Component
    {
        /// <summary>
        /// Defines the devour action
        /// </summary>
        [DataField("devourAction", required: true)]
        public EntityTargetAction DevourAction = new();

        // The amount of time it takes to devour something
        // NOTE: original inteded design was to increase this proportionaly with damage thresholds, but those proved quite difficult to get consistently.
        // right now it devours the structure at a fixed timer.
        [DataField("devourTime")]
        public float DevourTimer = 20f;

        //The amount of eggs the dragon is ready to hatch
        public int EggsLeft = 2;

        //Token for interrupting the action
        public CancellationTokenSource? CancelToken;
    }

    public sealed class DevourActionEvent : PerformEntityTargetActionEvent
    {
        
    }


}
