using System.Collections.Generic;
using Content.Server.Body.Mechanisms;
using Content.Server.GameObjects.Components.Body;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Body.Surgery
{
    /// <summary>
    ///     Interface representing an entity capable of performing surgery (performing operations on an
    ///     <see cref="SurgeryData"/> class).
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
        ///     When performing a surgery, the <see cref="SurgeryData"/> may sometimes require selecting from a set of Mechanisms
        ///     to operate on.
        ///     This function is called in that scenario, and it is expected that you call the callback with one mechanism from the
        ///     provided list.
        /// </summary>
        public void RequestMechanism(IEnumerable<IMechanism> options, MechanismRequestCallback callback);
    }
}
