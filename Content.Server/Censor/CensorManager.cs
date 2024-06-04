using System.Diagnostics;
using System.Text.RegularExpressions;
using Content.Shared.Censor;
using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Censor;

public sealed class CensorManager : ICensorManager, IPostInjectInit
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    private ISawmill _log = default!;

    // Filters
    private readonly Dictionary<CensorTarget, Dictionary<Regex, CensorFilter>> _regexCensors = new();

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        AddCensor(new CensorFilter("amogus",
            CensorFilterType.Regex,
            "warning",
            CensorTarget.IC | CensorTarget.OOC,
            "Amogus Censor"));
    }

    void IPostInjectInit.PostInject()
    {
        _log = _logMan.GetSawmill("censor");
    }

    public void AddCensor(CensorFilter censor)
    {
        foreach (CensorTarget targetFlag in Enum.GetValues(typeof(CensorTarget)))
        {
            if (targetFlag == CensorTarget.None)
                continue;
            if (!censor.TargetFlags.HasFlag(targetFlag))
                continue;

            if (censor.FilterType == CensorFilterType.Regex)
            {
                if (!_regexCensors.TryGetValue(targetFlag, out var list))
                {
                    list = new Dictionary<Regex, CensorFilter>();
                    _regexCensors.Add(targetFlag, list);
                }

                list.Add(new Regex(censor.FilterText), censor);
            }
            // TODO other filter types
        }
    }

    public bool RegexCensor(CensorTarget target, string inputText, ICommonSession session)
    {
        // Ensure that only 1 bit is set
        Debug.Assert(target != 0 && (target & (target - 1)) == 0);

        var blocked = true;

        // No censors defined for target
        if (!_regexCensors.TryGetValue(target, out var textCensors))
            return true;

        foreach (var (regex, textCensor) in textCensors)
        {
            var regexMatches = regex.Matches(inputText);
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
                _log.Error($"CensorActionGroupPrototype \"{textCensor.ActionGroup}\" not found.");
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
                blocked &= censorAction.RunAction(session, inputText, textMatches, textCensor, _entMan);
            }
        }

        return blocked;
    }
}
