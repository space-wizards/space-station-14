using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Censor;

public sealed class TextCensorActionDef(
    string regex,
    CensorFilterType filterType,
    string actionGroup,
    CensorTarget targets,
    string name)
{
    public string FilterText { get; } = regex;
    public CensorFilterType FilterType { get; } = filterType;
    public string ActionGroup { get; } = actionGroup;
    public CensorTarget TargetFlags { get; } = targets;
    public string DisplayName { get; } = name;
}

public enum CensorFilterType : byte
{
    PlainTextWords,
    FalsePositives,
    FalseNegatives,
    Regex,
}

[Flags]
public enum CensorTarget
{
    None  = 0,
    IC    = 1 << 0,
    OOC   = 1 << 1,
    Emote = 1 << 2,
    Name  = 1 << 3,
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
    /// <param name="censorTargetName">The name of the censor that matched the <paramref name="fullText"/>.</param>
    /// <param name="entMan"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        string censorTargetName,
        EntityManager entMan);

    // TODO ShadowCommander add a counter for each player that counts runs on an action for checking multiple slurs
    // in a certain time frame for auto banning and such
}
