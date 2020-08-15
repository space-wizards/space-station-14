using System.Collections.Generic;
using Content.Server.Health.BodySystem.Surgery.SurgeryData;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Health.BodySystem.Surgery.Surgeon
{

    /// <summary>
    ///     Interface representing an entity capable of performing surgery (performing operations on an <see cref="ISurgeryData"/> class).
    ///     For an example see <see cref="SurgeryToolComponent"/>, which inherits from this class.
    /// </summary>
    public interface ISurgeon
    {
        /// <summary>
        ///     How long it takes to perform a single surgery step (in seconds).
        /// </summary>
        public float BaseOperationTime { get; set; }


        public delegate void MechanismRequestCallback(Mechanism.Mechanism target, IBodyPartContainer container, ISurgeon surgeon, IEntity performer);

        /// <summary>
        ///     When performing a surgery, the <see cref="ISurgeryData"/> may sometimes require selecting from a set of Mechanisms to operate on.
        ///     This function is called in that scenario, and it is expected that you call the callback with one mechanism from the provided list.
        /// </summary>
        public void RequestMechanism(List<Mechanism.Mechanism> options, MechanismRequestCallback callback);
    }

}
