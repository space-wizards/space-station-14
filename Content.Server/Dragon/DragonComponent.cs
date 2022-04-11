using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using System.Threading;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed class DragonComponent : Component
    {
        /// <summary>
        /// The chemical ID injected upon devouring
        /// </summary>
        [DataField("devourChemical")]
        public string DevourChem = "Ichor";

        /// <summary>
        /// The amount of ichor injected per devour
        /// </summary>
        [DataField("devourHealRate")]
        public float DevourHealRate = 15f;

        /// <summary>
        /// Defines the devour action
        /// </summary>
        [DataField("devourAction", required: true)]
        public EntityTargetAction DevourAction = new();

        /// <summary>
        /// Defines the carp birthing action
        /// </summary>
        [DataField("carpBirthAction", required: true)]
        public InstantAction CarpBirthAction = new();

        // The amount of time it takes to devour something
        // NOTE: original inteded design was to increase this proportionaly with damage thresholds, but those proved quite difficult to get consistently.
        // right now it devours the structure at a fixed timer.
        [DataField("devourTime")]
        public float DevourTimer = 15f;

        /// <summary>
        /// The carp prototype
        /// </summary>
        [DataField("carpProto")]
        public string CarpProto = default!;

        /// <summary>
        /// The amount of carps the dragon is ready to hatch
        /// </summary>
        public int EggsLeft = 2;

        //Token for interrupting the action
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Where the entities go when dragon devours them, empties when the dragon is dead.
        /// </summary>
        public Container DragonStomach = default!;
    }

    public sealed class DevourActionEvent : PerformEntityTargetActionEvent
    {
        
    }

    public sealed class CarpBirthEvent: PerformActionEvent
    {

    }


}
