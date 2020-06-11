using Content.Shared.BodySystem;
using System;
using System.Collections.Generic;

namespace Content.Server.BodySystem
{

    /// <summary>
    ///     Interface representing an entity capable of performing surgery (performing operations on an <see cref="ISurgeryData">ISurgeryData</see> class).
    ///     For an example see <see cref="ServerSurgeryToolComponent">ServerSurgeryToolComponent</see>, which inherits from this class.
    /// </summary>	
    public interface ISurgeon
    {
        public float BaseOperationTime { get; set; }

        public delegate void MechanismRequestCallback(Mechanism target);
        public void RequestMechanism(List<Mechanism> options, MechanismRequestCallback callback);
    }

}
