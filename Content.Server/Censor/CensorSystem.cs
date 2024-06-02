using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Censor;
using Content.Shared.Chat.V2.Moderation;
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

    // Chat filters
    private readonly Dictionary<CensorTarget, SimpleCensor> _chatCensors = new();

    public override void Initialize()
    {
        base.Initialize();

        _censorActionDefs.Add(new TextCensorActionDef("amogus",
            CensorFilterType.PlainTextWords,
            new CensorActionGroupPrototype(),
            CensorTarget.IC | CensorTarget.OOC,
            "Amogus Censor"));
    }

    public void AddCensor(TextCensorActionDef censor)
    {
        foreach (CensorTarget targetFlag in Enum.GetValues(typeof(CensorTarget)))
        {
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

        foreach (var (censorTarget, filterList) in _censorActions)
        {
            // censored words
            var plainTextWords = new List<string>();
            foreach (var censoredWord in filterList[CensorFilterType.PlainTextWords])
            {
                plainTextWords.Add(censoredWord.FilterText);
            }
            // false positives
            // false negatives
        }
    }

    public void RegexCensor(CensorTarget target, string inputText, ICommonSession session)
    {
        // Ensure that only 1 bit is set
        Debug.Assert(target != 0 && (target & (target - 1)) == 0);

        var textCensors = _censorActions[target][CensorFilterType.Regex];
        foreach (var textCensor in textCensors)
        {
            var regexMatches = Regex.Matches(inputText, textCensor.FilterText);
            if (regexMatches.Count == 0)
                continue;

            var matches = regexMatches.ToList();

            var censorGroup = _protoMan.Index<CensorActionGroupPrototype>(textCensor.ActionGroup.ID);
            var stop = false;
            foreach (var censorAction in censorGroup.CensorActions)
            {
                if (censorAction.IsCensored(inputText, regexMatches))
                    continue;

                stop = true;
                break;
            }

            if (stop)
                continue;

            foreach (var censorAction in censorGroup.CensorActions)
            {
                censorAction.RunAction(session, inputText, regexMatches, textCensor.DisplayName, EntityManager);
            }
        }
    }
}
