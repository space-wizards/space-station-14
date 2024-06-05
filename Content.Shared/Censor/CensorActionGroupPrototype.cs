using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Censor;

/// <summary>
/// A censor filter for running actions when a match is found in user input.
/// </summary>
/// <param name="pattern">The text to find in user input.</param>
/// <param name="filterType">The type of filter to sort this definition into.</param>
/// <param name="actionGroup">The <seealso cref="CensorActionGroupPrototype"/> to run when the <paramref name="pattern"/> is matched</param>
/// <param name="targets">The user input types that this censor applies to.</param>
/// <param name="name">The user facing name of this censor.</param>
public sealed class CensorFilterDef(
    int? id,
    string pattern,
    CensorFilterType filterType,
    string actionGroup,
    CensorTarget targets,
    string name)
{
    public readonly int? Id = id;
    public readonly string Pattern = pattern;
    public readonly CensorFilterType FilterType = filterType;
    public readonly string ActionGroup = actionGroup;
    public readonly CensorTarget TargetFlags = targets;
    public readonly string DisplayName = name;

    public CensorFilterDef(
        string pattern,
        CensorFilterType filterType,
        string actionGroup,
        CensorTarget targets,
        string name) : this(null, pattern, filterType, actionGroup, targets, name)
    {
    }
}

/// <summary>
/// A group of actions to be run when a censor matches the text.
/// </summary>
[Prototype("censorActionGroup")]
public sealed class CensorActionGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ICensorAction> CensorActions = new();
}

public interface ICensorAction
{
    /// <summary>
    /// Check whether the actions should be run.
    /// </summary>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="matchedText">The text matched by the censor.</param>
    /// <returns>True when this censor should be skipped.</returns>
    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText);

    /// <summary>
    /// Run actions on the session.
    /// </summary>
    /// <param name="session">The session the text came from.</param>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="matchedText">The text matched by the censor.</param>
    /// <param name="censor">The censor that matched the <paramref name="fullText"/>.</param>
    /// <param name="entMan"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        CensorFilterDef censor,
        IEntityManager entMan);

    // TODO ShadowCommander add a counter for each player that counts runs on an action for checking multiple slurs
    // in a certain time frame for auto banning and such
}
