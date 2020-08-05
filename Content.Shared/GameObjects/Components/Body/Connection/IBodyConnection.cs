#nullable enable
using Content.Shared.GameObjects.Components.Body.Conduit;
using Content.Shared.GameObjects.Components.Body.Connector;
using Content.Shared.GameObjects.Components.Body.Valve;

namespace Content.Shared.GameObjects.Components.Body.Connection
{
    /// <summary>
    ///     Represents a connection that has been made within the body.
    /// </summary>
    public interface IBodyConnection
    {
        IBodyConnector? First { get; set; }
        
        IBodyConnector? Second { get; set; }

        (IBodyConnector?, IBodyConnector?) Connections => (First, Second);

        /// <summary>
        ///     The valve that this connection has, if any.
        /// </summary>
        IBodyValve? Valve { get; }

        bool TryPush(BodySubstanceType substance, int pressure, IBodyConnector towards);
    }
}
