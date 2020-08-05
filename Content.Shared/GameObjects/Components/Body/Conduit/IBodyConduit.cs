using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Connection;
using Content.Shared.GameObjects.Components.Body.Connector;
using Content.Shared.GameObjects.Components.Body.Substance;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Conduit
{
    /// <summary>
    ///     Represents a conduit within the body.
    /// </summary>
    public interface IBodyConduit : IBodyConnector
    {
        /// <summary>
        ///     The id of this conduit.
        /// </summary>
        string ConduitId { get; }

        /// <summary>
        ///     The name to display to the player.
        /// </summary>
        string ConduitName { get; }

        /// <summary>
        ///     The type of substances that this conduit naturally lets through.
        /// </summary>
        BodySubstanceType Type { get; set; }

        /// <summary>
        ///     The maximum volume of this conduit in millilitres.
        ///     To get the volume in fluid ounces <see cref="MaxVolumeUsOunces"/>.
        /// </summary>
        int MaxVolume { get; set; }

        /// <summary>
        ///     The maximum volume of this conduit in U.S. fluid ounces.
        ///     For interface purposes only.
        ///     For any kind of computation <see cref="MaxVolume"/>.
        /// </summary>
        double MaxVolumeUsOunces => MaxVolume * 0.033814023;

        // TODO
        string Part { get; set; }
        
        // TODO
        List<BodyConnection> Connections { get; set; }

        void Initialize();

        /// <summary>
        ///     Checks if this <see cref="IBodyConduit"/> can accept some substance.
        /// </summary>
        /// <param name="substance">The substance to accept.</param>
        /// <returns></returns>
        bool Accepts(IBodySubstance substance)
        {
            return Type >= substance.Type;
        }

        /// <summary>
        ///     Creates a copy of this <see cref="IBodyConduit"/>.
        /// </summary>
        /// <returns>The copy.</returns>
        IBodyConduit Copy();
    }

    public enum BodySubstanceType
    {
        None = 0,
        Gas = 1,
        Liquid = 2,
        Solid = 3
    }
}
