using Robust.Shared.Audio;

namespace Content.Server.Disease.Events;

/// <summary>
///     Raised by an entity about to sneeze/cough.
/// </summary>
//[ByRefEvent]
public sealed class AttemptSneezeCoughEvent : EntityEventArgs
{

    /// <summary>
    /// The mob about to cough.
    /// </summary>
    public EntityUid Uid { get; }

    /// <summary>
    /// Message to play when snoughing
    /// </summary>
    public string SnoughMessage;

    /// </summary>
    /// Sound to play when snoughing
    /// <summary>
    public SoundSpecifier? SnoughSound;

    /// </summary>
    /// Interrupts the snough if true
    /// <summary>
    public bool Cancelled = false;

    public AttemptSneezeCoughEvent(EntityUid uid, string snoughMessage, SoundSpecifier? snoughSound)
    {
        Uid = uid;
        SnoughMessage = snoughMessage;
        SnoughSound = snoughSound;
    }
}