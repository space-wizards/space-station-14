namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     For components that are updated by <see cref="PipeNetDeviceComponent"/>.
    /// </summary>
    public interface IPipeNetUpdated
    {
        /// <summary>
        ///     Triggers gas processing on this component.
        /// </summary>
        void Update(PipeNetUpdateMessage message);
    }

    public class PipeNetUpdateMessage
    {

    }
}
