using Content.Shared.CCVar;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tips;

/// <summary>
/// Handles periodically displaying gameplay tips to all players ingame.
/// </summary>
public abstract class SharedTipsSystem : EntitySystem
{
    /// <summary>
    /// Always adds this time to a speech message. This is so really short message stay around for a bit.
    /// </summary>
    private const float SpeechBuffer = 3f;

    /// <summary>
    /// Expected reading speed.
    /// </summary>
    private const float Wpm = 180f;

    /// <summary>
    /// Send a tippy message to all clients.
    /// </summary>
    /// <param name="message">The text to show in the speech bubble.</param>
    /// <param name="prototype">The entity to show. Defaults to tippy.</param>
    /// <param name="speakTime">The time the speech bubble is shown, in seconds.</param>
    /// <param name="slideTime">The time the entity takes to walk onto the screen, in seconds.</param>
    /// <param name="waddleInterval">The time between waddle animation steps, in seconds.</param>
    public virtual void SendTippy(
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    { }

    /// <summary>
    /// Send a tippy message to the given player session.
    /// </summary>
    /// <param name="session">The player session to send the message to.</param>
    /// <param name="message">The text to show in the speech bubble.</param>
    /// <param name="prototype">The entity to show. Defaults to tippy.</param>
    /// <param name="speakTime">The time the speech bubble is shown, in seconds.</param>
    /// <param name="slideTime">The time the entity takes to walk onto the screen, in seconds.</param>
    /// <param name="waddleInterval">The time between waddle animation steps, in seconds.</param>
    public virtual void SendTippy(
        ICommonSession session,
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    { }

    /// <summary>
    /// Send a random tippy message from the dataset given in <see cref="CCVars.TipsDataset"/>.
    /// </summary>
    public virtual void AnnounceRandomTip() { }

    /// <summary>
    /// Set a random time stamp for the next automatic game tip.
    /// </summary>
    public virtual void RecalculateNextTipTime() { }

    /// <summary>
    /// Calculate the recommended speak time for a given message.
    /// </summary>
    public float GetSpeechTime(string text)
    {
        var wordCount = (float)text.Split().Length;
        return SpeechBuffer + wordCount * (60f / Wpm);
    }
}
