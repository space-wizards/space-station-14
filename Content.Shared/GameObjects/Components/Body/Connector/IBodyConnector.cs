namespace Content.Shared.GameObjects.Components.Body.Connector
{
    /// <summary>
    ///     Represents anything that can be connected to a conduit or directly with
    ///     another connector within the body.
    /// </summary>
    public interface IBodyConnector
    {
        bool TryConnect(IBodyConnector other);
    }
}
