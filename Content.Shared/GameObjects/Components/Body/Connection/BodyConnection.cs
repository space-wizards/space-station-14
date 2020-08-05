#nullable enable
using Content.Shared.GameObjects.Components.Body.Conduit;
using Content.Shared.GameObjects.Components.Body.Connector;
using Content.Shared.GameObjects.Components.Body.Valve;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Connection
{
    public class BodyConnection : IBodyConnection
    {
        [ViewVariables]
        public IBodyConnector? First { get; set; }

        [ViewVariables] public IBodyConnector? Second { get; set; }

        [ViewVariables] public IBodyValve? Valve { get; }

        // TODO
        public bool TryPush(BodySubstanceType substance, int pressure, IBodyConnector towards)
        {
            throw new System.NotImplementedException();
        }
    }
}