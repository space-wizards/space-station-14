using System.Threading;
using Content.Shared.Disease;

namespace Content.Server.Disease.Components
{
    /// <summary>
    /// For mouth swabs used to collect and process
    /// disease samples.
    /// </summary>
    [RegisterComponent]
    public sealed class DiseaseSwabComponent : Component
    {
        /// <summary>
        /// How long it takes to swab someone.
        /// </summary>
        [DataField("swabDelay")]
        public float SwabDelay = 2f;
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
