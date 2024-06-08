using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    private readonly Dictionary<AutomodTarget, List<RegexAutomodFilterDef>> _regexFilters = new();

    #region Initialize

    public void Initialize()
    {
        LoadAutomodFiltersFromDatabase();
    }

    private async void LoadAutomodFiltersFromDatabase()
    {
        var filters = await _db.GetAllAutomodFiltersAsync();
        foreach (var filter in filters)
        {
            AddFilter(filter);
        }
    }

    void IPostInjectInit.PostInject()
    {
        _log = _logMan.GetSawmill("automod");
    }

    #endregion

    #region Filter

    public bool Filter(AutomodTarget target, string inputText, ICommonSession session)
    {
        return RegexFilter(target, inputText, session);
    }

    /// <inheritdoc cref="Filter"/>
    private bool RegexFilter(AutomodTarget target, string inputText, ICommonSession session)
    {
        // Ensure that only 1 bit is set
        Debug.Assert(target != 0 && (target & (target - 1)) == 0);

        var passes = true;

        // No filters defined for target
        if (!_regexFilters.TryGetValue(target, out var automodFilters))
            return true;

        foreach (var filter in automodFilters)
        {
            var regexMatches = filter.Regex.Matches(inputText);
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

            var displayName = GetDisplayName(filter);

            foreach (var censorAction in censorGroup.AutomodActions)
            {
                passes &= censorAction.RunAction(session, inputText, textMatches, filter, displayName, _entMan);
            }
        }

        return passes;
    }

    private string GetDisplayName(AutomodFilterDef filter)
    {
        return filter.Id != null ? $"{filter.DisplayName} (automod#{filter.Id})" : $"{filter.DisplayName} (automod#na)";
    }

    #endregion

    #region Reload

    public async void ReloadAutomodFilters()
    {
        _regexFilters.Clear();

        var filters = await _db.GetAllAutomodFiltersAsync();

        foreach (var filter in filters)
        {
            AddFilter(filter);
        }
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
                    list = new List<RegexAutomodFilterDef>();
                    _regexFilters.Add(targetFlag, list);
                }

                list.Add(new RegexAutomodFilterDef(automod, new Regex(automod.Pattern)));
            }
            // TODO other filter types
        }
    }

    #endregion

    #region Get

    public async Task<AutomodFilterDef?> GetFilter(int id)
    {
        return await _db.GetAutomodFilterAsync(id);
    }

    #endregion

    #region Edit

    public void EditFilter(AutomodFilterDef automodFilterDef)
    {
        _db.EditAutomodFilterAsync(automodFilterDef);
    }

    #endregion

    #region Remove

    public async Task<bool> RemoveFilter(int id)
    {
        RegexRemoveFiltersById([id]);

        return await _db.RemoveAutomodFilterAsync(id);
    }

    public async Task RemoveMultipleFilters(List<int> ids)
    {
        RegexRemoveFiltersById(ids);

        await _db.RemoveMultipleAutomodFilterAsync(ids);
    }

    private void RegexRemoveFiltersById(List<int> ids)
    {
        var toRemoveTargets = new List<AutomodTarget>();
        foreach (var (target, reg) in _regexFilters)
        {
            var toRemove = new List<RegexAutomodFilterDef>();
            foreach (var filter in reg)
            {
                if (filter.Id == null || !ids.Contains(filter.Id.Value))
                    continue;

                toRemove.Add(filter);
            }
            foreach (var regex in toRemove)
            {
                reg.Remove(regex);
            }

            if (reg.Count == 0)
                toRemoveTargets.Add(target);
        }

        foreach (var target in toRemoveTargets)
        {
            _regexFilters.Remove(target);
        }
    }

    #endregion
}
