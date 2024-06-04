using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Censor;

public sealed class CensorFilter(
    string regex,
    CensorFilterType filterType,
    string actionGroup,
    CensorTarget targets,
    string name)
{
    public readonly string FilterText = regex;
    public readonly CensorFilterType FilterType = filterType;
    public readonly string ActionGroup = actionGroup;
    public readonly CensorTarget TargetFlags = targets;
    public readonly string DisplayName = name;
}

/// <summary>
/// A group of actions to be run when a censor matches the text.
/// </summary>
[Prototype]
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
        CensorFilter censor,
        IEntityManager entMan);

    // TODO ShadowCommander add a counter for each player that counts runs on an action for checking multiple slurs
    // in a certain time frame for auto banning and such
}
