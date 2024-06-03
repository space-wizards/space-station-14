using Content.Shared.Censor;
using Robust.Shared.Player;

namespace Content.Server.Censor;

public interface ICensorManager
{
    void Initialize();

    /// <summary>
    /// Add a censor def to the manager.
    /// This method sorts the definitions for faster lookup.
    /// </summary>
    /// <param name="censor">The censor to add.</param>
    void AddCensor(TextCensorActionDef censor);

    /// <summary>
    /// Checks a message for any matching regex censors. If there is a match, it runs ICensorActions on the text and matches.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="inputText"></param>
    /// <param name="session"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    bool RegexCensor(CensorTarget emote, string message, ICommonSession session);
}
