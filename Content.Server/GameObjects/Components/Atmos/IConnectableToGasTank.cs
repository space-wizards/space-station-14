#nullable enable
namespace Content.Server.GameObjects.Components.Atmos
{
    public interface IConnectableToGasTank
    {
        public GasTankComponent? ConnectedGasTank { get; set; }
        public void Disconnect();
        public void Connect();
    }
}
