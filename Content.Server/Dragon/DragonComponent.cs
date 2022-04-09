using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
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

        // The amount of time it takes to devour something
        // NOTE: original inteded design was to increase this proportionaly with damage thresholds, but those proved quite difficult to get consistently.
        // right now it devours the structure at a fixed timer.
        [DataField("devourTime")]
        public float DevourTimer = 15f;

        //The amount of eggs the dragon is ready to hatch
        public int EggsLeft = 2;

        //Token for interrupting the action
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Where the entities go when dragon devours them, ruptures when the dragon is dead.
        /// </summary>
        public Container DragonStomach = default!;
    }

    public sealed class DevourActionEvent : PerformEntityTargetActionEvent
    {
        
    }


}
