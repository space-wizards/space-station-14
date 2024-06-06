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
    private readonly Dictionary<AutomodTarget, Dictionary<Regex, AutomodFilterDef>> _regexFilters = new();

    #region Initialize

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

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

            var displayName = GetDisplayName(filter);

            foreach (var censorAction in censorGroup.AutomodActions)
            {
                blocked &= censorAction.RunAction(session, inputText, textMatches, filter, displayName, _entMan);
            }
        }

        return blocked;
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
                    list = new Dictionary<Regex, AutomodFilterDef>();
                    _regexFilters.Add(targetFlag, list);
                }

                list.Add(new Regex(automod.Pattern), automod);
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
        var toRemoveTargets = new List<AutomodTarget>();
        Regex? toRemove = null;
        foreach (var (target, reg) in _regexFilters)
        {
            foreach (var (regex, filter) in reg)
            {
                if (filter.Id == null || filter.Id.Value != id)
                    continue;

                toRemove = regex;
                break;
            }

            if (toRemove != null)
                reg.Remove(toRemove);
            toRemove = null;

            if (reg.Count == 0)
                toRemoveTargets.Add(target);
        }

        foreach (var target in toRemoveTargets)
        {
            _regexFilters.Remove(target);
        }

        return await _db.RemoveAutomodFilterAsync(id);
    }

    public async Task RemoveMultipleFilters(List<int> ids)
    {

        var toRemoveTargets = new List<AutomodTarget>();
        var toRemove = new List<Regex>();
        foreach (var (target, reg) in _regexFilters)
        {
            foreach (var (regex, filter) in reg)
            {
                if (filter.Id == null || !ids.Contains(filter.Id.Value))
                    continue;

                toRemove.Add(regex);
            }

            foreach (var regex in toRemove)
            {
                reg.Remove(regex);
            }

            toRemove.Clear();

            if (reg.Count == 0)
                toRemoveTargets.Add(target);
        }

        foreach (var target in toRemoveTargets)
        {
            _regexFilters.Remove(target);
        }

        await _db.RemoveMultipleAutomodFilterAsync(ids);
    }

    #endregion
}
