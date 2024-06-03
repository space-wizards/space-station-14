using System.Diagnostics;
using System.Text.RegularExpressions;
using Content.Shared.Censor;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Censor;

public sealed class CensorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    // Loaded from database
    private readonly List<TextCensorActionDef> _censorActionDefs = new();

    // Cache for faster lookup
    private readonly Dictionary<CensorTarget, Dictionary<CensorFilterType, List<TextCensorActionDef>>> _censorActions = new();

    // Filters
    private readonly Dictionary<CensorTarget, Dictionary<Regex, CensorActionGroupPrototype>> _regexCensors = new();
    // private readonly Dictionary<CensorTarget, SimpleCensor> _chatCensors = new();

    public override void Initialize()
    {
        base.Initialize();

        AddCensor(new TextCensorActionDef("amogus",
            CensorFilterType.Regex,
            "warning",
            CensorTarget.IC | CensorTarget.OOC,
            "Amogus Censor"));
    }

    public void AddCensor(TextCensorActionDef censor)
    {
        foreach (CensorTarget targetFlag in Enum.GetValues(typeof(CensorTarget)))
        {
            if (targetFlag == CensorTarget.None)
                continue;
            if (!censor.TargetFlags.HasFlag(targetFlag))
                continue;

            // Add to list
            #region Add to list
            if (!_censorActions.TryGetValue(targetFlag, out var filterList))
            {
                filterList = new Dictionary<CensorFilterType, List<TextCensorActionDef>>();
                _censorActions.Add(targetFlag, filterList);
            }

            if (!filterList.TryGetValue(censor.FilterType, out var list))
            {
                list = new List<TextCensorActionDef>();
                filterList.Add(censor.FilterType, list);
            }

            list.Add(censor);
            #endregion
        }

        // foreach (var (censorTarget, filterList) in _censorActions)
        // {
        //     var regexCensors = new Dictionary<Regex, CensorActionGroupPrototype>();
        //     // TODO create Regex list
        //     foreach (var textCensorActionDef in filterList[CensorFilterType.Regex])
        //     {
        //         regexCensors.Add(new Regex(textCensorActionDef.FilterText), textCensorActionDef.ActionGroup);
        //     }
        //     _regexCensors.Add(censorTarget, regexCensors);
        //
        //     // censored words
        //     var plainTextWords = new List<string>();
        //     foreach (var censoredWord in filterList[CensorFilterType.PlainTextWords])
        //     {
        //         plainTextWords.Add(censoredWord.FilterText);
        //     }
        //     // false positives
        //     // false negatives
        // }
    }

    /// <summary>
    /// Checks a message for any matching regex censors. If there is a match, it runs ICensorActions on the text and matches.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="inputText"></param>
    /// <param name="session"></param>
    /// <returns>True if the message passes. False if the message should be blocked.</returns>
    public bool RegexCensor(CensorTarget target, string inputText, ICommonSession session)
    {
        // Ensure that only 1 bit is set
        Debug.Assert(target != 0 && (target & (target - 1)) == 0);

        var blocked = true;

        // No censors defined for target
        if (!_censorActions.TryGetValue(target, out var textCensorActionDefs))
            return true;

        // No regex censors defined
        if (!textCensorActionDefs.TryGetValue(CensorFilterType.Regex, out var textCensors))
            return true;

        foreach (var textCensor in textCensors)
        {
            var regexMatches = Regex.Matches(inputText, textCensor.FilterText);
            if (regexMatches.Count == 0)
                continue;

            var textMatches = new Dictionary<string, int>();
            foreach (Match match in regexMatches)
            {
                var str = match.ToString();
                textMatches.Add(str, match.Index);
            }

            if (!_protoMan.TryIndex<CensorActionGroupPrototype>(textCensor.ActionGroup, out var censorGroup))
            {
                Log.Error($"CensorActionGroupPrototype \"{textCensor.ActionGroup}\" not found.");
                continue;
            }

            var skip = false;
            foreach (var censorAction in censorGroup.CensorActions)
            {
                if (!censorAction.SkipCensor(inputText, textMatches))
                    continue;

                skip = true;
                break;
            }

            if (skip)
                continue;

            foreach (var censorAction in censorGroup.CensorActions)
            {
                blocked &= censorAction.RunAction(session, inputText, textMatches, textCensor.DisplayName, EntityManager);
            }
        }

        return blocked;
    }
}
