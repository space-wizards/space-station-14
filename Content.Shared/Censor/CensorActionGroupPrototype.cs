using System.Text.RegularExpressions;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Censor;

public sealed class TextCensorActionDef(
    string regex,
    CensorFilterType filterType,
    CensorActionGroupPrototype actionGroup,
    CensorTarget targets,
    string name)
{
    public string FilterText { get; } = regex;
    public CensorFilterType FilterType { get; } = filterType;
    public CensorActionGroupPrototype ActionGroup { get; } = actionGroup;
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
public enum CensorTarget : int
{
    None  = 0,
    IC    = 1 << 0,
    OOC   = 1 << 0,
    Emote = 1 << 0,
    Name  = 1 << 0,
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

    public List<ICensorAction> CensorActions = new();
}

public interface ICensorAction
{
    /// <summary>
    /// Check whether the actions should be run.
    /// </summary>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="matchedText">The text matched by the censor.</param>
    /// <returns></returns>
    public bool AttemptCensor(string fullText, Dictionary<string, int> matchedText);

    /// <summary>
    /// Run actions on the session.
    /// </summary>
    /// <param name="session">The session the text came from.</param>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="matchedText">The text matched by the censor.</param>
    /// <param name="censorTargetName">The name of the censor that matched the <paramref name="fullText"/>.</param>
    /// <param name="entMan"></param>
    public void RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        string censorTargetName,
        EntityManager entMan);
}
