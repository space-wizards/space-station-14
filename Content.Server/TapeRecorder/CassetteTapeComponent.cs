using System.Threading;

namespace Content.Server.TapeRecorder
{

    /// <summary>
    /// Component given to cassette tapes, these hold the information recorded onto them by tape recorders.
    /// </summary>
    [RegisterComponent]
    public sealed class CassetteTapeComponent : Component
    {
        /// <summary>
        /// A list of all recorded messages with timestamps
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public List<(float MessageTimeStamp, string Message)> RecordedMessages = new ();

        public CancellationTokenSource? CancelToken = null;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("unspooled")]
        public bool Unspooled = false;

        /// <summary>
        ///Our current position in the "tape"
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float TimeStamp;

        /// <summary>
        /// The maximum length of the tape
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("tapeMaxTime")]
        public float TapeMaxTime = 20f;
    }
}
