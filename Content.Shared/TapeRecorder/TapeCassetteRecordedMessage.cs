using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.TapeRecorder;

/// <summary>
/// Every chat event recorded on a tape is saved in this format
/// </summary>
public sealed class TapeCassetteRecordedMessage
{
    /// <summary>
    /// Number of seconds since the start of the tape that this event was recorded at
    /// </summary>
    public float Timestamp { get; private set; }

    /// <summary>
    /// The name of the entity that spoke
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// What was spoken
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// If this message has been flushed to the tape, used to handle erasing of old entries
    /// </summary>
    public bool Flushed { get; set; }

    public TapeCassetteRecordedMessage(float timestamp, string name, string message)
    {
        this.Timestamp = timestamp;
        this.Name = name;
        this.Message = message;
        this.Flushed = false;
    }
}
