using Content.Shared.MassDriver;

namespace Content.Client.MassDriver
{
    [RegisterComponent]
    public sealed class MassDriverComponent : SharedMassDriverComponent
    {
        /// <summary>
        /// The sprite state for when the driver is ready to be launched.
        /// </summary>
        [DataField("readyState")]
        public string ReadyState = "ready";

        /// <summary>
        /// The sprite state for when the driver is currently in the launching animation.
        /// </summary>
        [DataField("launchingState")]
        public string LaunchingState = "launching";

        /// <summary>
        /// The sprite state for when the driver has finished the launching
        /// animation and is waiting to be retracted after cooldown.
        /// </summary>
        [DataField("launchedState")]
        public string LaunchedState = "launched";

        /// <summary>
        /// The sprite state for when the driver is currently in the retracting
        /// animation before going back to being ready.
        /// </summary>
        [DataField("retractingState")]
        public string RetractingState = "retracting";
    }

    public enum MassDriverVisualLayers : byte
    {
        Base,
    }
}
