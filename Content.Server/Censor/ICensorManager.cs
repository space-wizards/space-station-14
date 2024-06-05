using Content.Shared.Censor;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server.Censor;

public interface ICensorManager
{
    public void Initialize();

    /// <summary>
    /// <inheritdoc cref="CreateCensor(CensorFilterDef)"/>
    /// </summary>
    /// <inheritdoc cref="CensorFilterDef"/>
    public void CreateCensor(string filter,
        CensorFilterType filterType,
        string actionGroup,
        CensorTarget targets,
        string name);

    /// <summary>
    /// Add a censor def to the manager.
    /// This method sorts the definitions for faster lookup.
    /// </summary>
    /// <param name="censor">The censor to add.</param>
    public void CreateCensor(CensorFilterDef censor);

    /// <summary>
    /// Checks a message for any matching regex censors. If there is a match, it runs ICensorActions on the text and matches.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="inputText"></param>
    /// <param name="session"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool RegexCensor(CensorTarget emote, string message, ICommonSession session);

    /// <summary>
    /// Clears and reloads all censors from the database.
    /// </summary>
    public void ReloadCensors();
}
