using Robust.Shared.Serialization;

namespace Content.Shared.MassDriver
{
    public abstract class SharedMassDriverComponent : Component
    {
        /// <summary>
        /// How long is the Launching animation?
        /// </summary>
        /// <remarks>
        /// This is used by the client for animation and by the server for
        /// state rotation.
        /// </remarks>
        [DataField("launchDelay")]
        public TimeSpan LaunchDelay = TimeSpan.FromMilliseconds(700);

        /// <summary>
        /// How long is the Retracting animation?
        /// </summary>
        /// <remarks>
        /// This is used by the client for animation and by the server for
        /// state rotation.
        /// </remarks>
        [DataField("retractDelay")]
        public TimeSpan RetractDelay = TimeSpan.FromMilliseconds(400);
    }

    /// <remarks>
    /// This is used both for visual data and actual component state.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum MassDriverState : byte
    {
        Ready,
        Launching,
        Launched,
        Retracting,
    }
}
