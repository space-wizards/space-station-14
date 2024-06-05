using System.Diagnostics;
using System.Text.RegularExpressions;
using Content.Server.Database;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Automod;

public sealed class AutomodManager : IAutomodManager, IPostInjectInit
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private ISawmill _log = default!;

    // Filters
    private readonly Dictionary<AutomodTarget, Dictionary<Regex, AutomodFilterDef>> _regexFilters = new();

    #region Initialize

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        LoadAutomodFiltersFromDatabase();
    }

    private async void LoadAutomodFiltersFromDatabase()
    {
        var censors = await _db.GetAllAutomodFiltersAsync();
        foreach (var censor in censors)
        {
            AddFilter(censor);
        }
    }

    void IPostInjectInit.PostInject()
    {
        _log = _logMan.GetSawmill("censor");
    }

    #endregion

    #region Create

    public async void CreateFilter(string pattern,
        AutomodFilterType filterType,
        string actionGroup,
        AutomodTarget targets,
        string name)
    {
        CreateFilter(new AutomodFilterDef(pattern, filterType, actionGroup, targets, name));
    }

    public async void CreateFilter(AutomodFilterDef automod)
    {
        automod = await _db.AddAutomodFilterAsync(automod);
        AddFilter(automod);
    }

    private void AddFilter(AutomodFilterDef automod)
    {
        foreach (AutomodTarget targetFlag in Enum.GetValues(typeof(AutomodTarget)))
        {
            if (targetFlag == AutomodTarget.None)
                continue;
            if (!automod.TargetFlags.HasFlag(targetFlag))
                continue;

            if (automod.FilterType == AutomodFilterType.Regex)
            {
                if (!_regexFilters.TryGetValue(targetFlag, out var list))
                {
                    list = new Dictionary<Regex, AutomodFilterDef>();
                    _regexFilters.Add(targetFlag, list);
                }

                list.Add(new Regex(automod.Pattern), automod);
            }
            // TODO other filter types
        }
    }

    #endregion

    #region Filter

    public bool Filter(AutomodTarget target, string inputText, ICommonSession session)
    {
        return RegexFilter(target, inputText, session);
    }

    private bool RegexFilter(AutomodTarget target, string inputText, ICommonSession session)
    {
        // Ensure that only 1 bit is set
        Debug.Assert(target != 0 && (target & (target - 1)) == 0);

        var blocked = true;

        // No censors defined for target
        if (!_regexFilters.TryGetValue(target, out var automodFilters))
            return true;

        foreach (var (regex, filter) in automodFilters)
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

            if (!_protoMan.TryIndex<AutomodActionGroupPrototype>(filter.ActionGroup, out var censorGroup))
            {
                _log.Error($"AutomodActionGroupPrototype \"{filter.ActionGroup}\" not found.");
                continue;
            }

            var skip = false;
            foreach (var censorAction in censorGroup.AutomodActions)
            {
                if (!censorAction.Skip(inputText, textMatches))
                    continue;

                skip = true;
                break;
            }

            if (skip)
                continue;

            foreach (var censorAction in censorGroup.AutomodActions)
            {
                blocked &= censorAction.RunAction(session, inputText, textMatches, filter, _entMan);
            }
        }

        return blocked;
    }

    #endregion

    #region Reload

    public async void ReloadAutomodFilters()
    {
        var censors = await _db.GetAllAutomodFiltersAsync();

        foreach (var censor in censors)
        {
            AddFilter(censor);
        }
    }

    #endregion
}
