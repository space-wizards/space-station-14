using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Automod;

/// <summary>
/// An automod filter for running actions when a pattern match is found in user input.
/// </summary>
/// <param name="pattern">The text to find in user input.</param>
/// <param name="filterType">The type of filter to sort this definition into.</param>
/// <param name="actionGroup">The <seealso cref="AutomodActionGroupPrototype"/> to run when the <paramref name="pattern"/> is matched</param>
/// <param name="targets">The user input types that this filter applies to.</param>
/// <param name="name">The user facing name of this filter.</param>
public sealed class AutomodFilterDef(
    int? id,
    string pattern,
    AutomodFilterType filterType,
    string actionGroup,
    AutomodTarget targets,
    string name)
{
    public readonly int? Id = id;
    public readonly string Pattern = pattern;
    public readonly AutomodFilterType FilterType = filterType;
    public readonly string ActionGroup = actionGroup;
    public readonly AutomodTarget TargetFlags = targets;
    public readonly string DisplayName = name;

    public AutomodFilterDef(
        string pattern,
        AutomodFilterType filterType,
        string actionGroup,
        AutomodTarget targets,
        string name) : this(null, pattern, filterType, actionGroup, targets, name)
    {
    }
}

/// <summary>
/// A group of actions to be run when the automod pattern matches the text.
/// </summary>
[Prototype("automodActionGroup")]
public sealed class AutomodActionGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ITextAutomodAction> AutomodActions = new();
}

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
[ImplicitDataDefinitionForInheritors]
public partial interface ITextAutomodAction
{
    /// <summary>
    /// Check whether the actions should be run.
    /// </summary>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="patternMatches">The text matched by the pattern.</param>
    /// <returns>True when this filter's actions should be skipped.</returns>
    public bool Skip(string fullText, Dictionary<string, int> patternMatches);

    /// <summary>
    /// Run actions on the session.
    /// </summary>
    /// <param name="session">The session the text came from.</param>
    /// <param name="fullText">The full text provided by the user.</param>
    /// <param name="patternMatches">The text matched by the pattern.</param>
    /// <param name="filter">The filter def that matched the <paramref name="fullText"/>.</param>
    /// <param name="filterDisplayName">The display ID of the automod filter.</param>
    /// <param name="entMan"><see cref="IEntityManager"/> for resolving systems in the action.</param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> patternMatches,
        AutomodFilterDef filter,
        string filterDisplayName,
        IEntityManager entMan);

    // TODO ShadowCommander add a counter for each player that counts runs on an action for checking multiple hits
    // in a certain time frame for auto banning and such
}
