using System.Threading;
using Content.Shared.Disease;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// For the swabs you use to take samples of diseases
    public class DiseaseSwabComponent : Component
    {
        /// <summary>
        /// How long it takes to swab someone.
        /// </summary>
        [DataField("swabDelay")]
        [ViewVariables]
        public float SwabDelay = 0.8f;


        /// <summary>
        /// If this swab has been used
        /// </summary>
        public bool Used = false;

        /// <summary>
        /// Token for interrupting swabbing do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// The disease prototype currently on the swab
        /// </summary>
        [ViewVariables]
        public DiseasePrototype? Disease;
    }
}
