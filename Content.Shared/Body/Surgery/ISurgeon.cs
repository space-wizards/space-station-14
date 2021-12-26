using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery
{
    /// <summary>
    ///     Interface representing an entity capable of performing surgery,
    ///     such as a circular saw.
    /// </summary>
    public interface ISurgeon
    {
        public delegate void MechanismRequestCallback(
            SharedMechanismComponent target,
            IBodyPartContainer container,
            ISurgeon surgeon,
            EntityUid performer);

        /// <summary>
        ///     How long it takes to perform a single surgery step in seconds.
        /// </summary>
        public float BaseOperationTime { get; set; }

        /// <summary>
        ///     When performing a surgery, the <see cref="SurgeryDataComponent"/>
        ///     may sometimes require selecting from a set of
        ///     <see cref="SharedMechanismComponent"/>s to operate on.
        ///     This function is called in that scenario, and it is expected that you call
        ///     the callback with one <see cref="SharedMechanismComponent"/> from the provided list.
        /// </summary>
        public void RequestMechanism(IEnumerable<SharedMechanismComponent> options, MechanismRequestCallback callback);
    }
}
