using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    /// <summary>
    ///     Interface representing an entity capable of performing surgery (performing operations on an
    ///     <see cref="SurgeryDataComponent"/> class).
    ///     For an example see <see cref="SurgeryToolComponent"/>, which inherits from this class.
    /// </summary>
    public interface ISurgeon
    {
        public delegate void MechanismRequestCallback(
            IMechanism target,
            IBodyPartContainer container,
            ISurgeon surgeon,
            IEntity performer);

        /// <summary>
        ///     How long it takes to perform a single surgery step (in seconds).
        /// </summary>
        public float BaseOperationTime { get; set; }

        /// <summary>
        ///     When performing a surgery, the <see cref="SurgeryDataComponent"/> may sometimes require selecting from a set of Mechanisms
        ///     to operate on.
        ///     This function is called in that scenario, and it is expected that you call the callback with one mechanism from the
        ///     provided list.
        /// </summary>
        public void RequestMechanism(IEnumerable<IMechanism> options, MechanismRequestCallback callback);
    }
}
