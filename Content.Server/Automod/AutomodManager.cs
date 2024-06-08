using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Automod;
using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
        ReloadAutomodFilters();
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
        Debug.Assert(BitOperations.PopCount((uint)target) == 1);

        var passes = true;

        // No filters defined for target
        if (!_regexFilters.TryGetValue(target, out var regexFilters))
            return true;

        foreach (var regexFilter in regexFilters)
        {
            var regexMatches = regexFilter.Regex.Matches(inputText);
            if (regexMatches.Count == 0)
                continue;

            var textMatches = new List<(string, int)>();
            foreach (Match match in regexMatches)
            {
                var str = match.ToString();
                textMatches.Add((str, match.Index));
            }

            if (!_protoMan.TryIndex<AutomodActionGroupPrototype>(regexFilter.Filter.ActionGroup, out var censorGroup))
            {
                _log.Error($"AutomodActionGroupPrototype \"{regexFilter.Filter.ActionGroup}\" not found.");
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

            var displayName = GetDisplayName(regexFilter.Filter);

            foreach (var censorAction in censorGroup.AutomodActions)
            {
                passes &= censorAction.RunAction(session, inputText, textMatches, regexFilter.Filter, displayName, _entMan);
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

    public async Task<string?> CreateFilter(AutomodFilterDef automod)
    {
        if (automod.FilterType == AutomodFilterType.Regex
            && !TryParseRegex(automod, out _, out var error))
            return error;

        automod = await _db.AddAutomodFilterAsync(automod);

        return AddFilter(automod);
    }

    /// <summary>
    /// Adds a filter to the manager lists.
    /// </summary>
    /// <param name="automod"></param>
    private string? AddFilter(AutomodFilterDef automod)
    {
        foreach (var targetFlag in Enum.GetValues<AutomodTarget>())
        {
            if (targetFlag == AutomodTarget.None)
                continue;
            if (!automod.TargetFlags.HasFlag(targetFlag))
                continue;

            if (automod.FilterType == AutomodFilterType.Regex)
            {
                var list = _regexFilters.GetOrNew(targetFlag);

                if (!TryParseRegex(automod, out var regex, out var error))
                    return error;

                list.Add(new RegexAutomodFilterDef(automod, regex));
            }
            // TODO other filter types
        }

        return null;
    }

    private bool TryParseRegex(AutomodFilterDef automod, [NotNullWhen(true)] out Regex? regex, out string? error)
    {
        regex = null;
        error = null;
        try
        {
            regex = new Regex(automod.Pattern);
        }
        catch (RegexParseException e)
        {
            error = $"Failed to parse automod regex filter: {GetDisplayName(automod)
                }. Pattern: \"{automod.Pattern}\". Error code: {e.Error}.";
            _log.Error(error);
            return false;
        }

        return true;
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

    /// <summary>
    /// Removes all filters that match any of the <paramref name="ids"/> from <see cref="_regexFilters"/>.
    /// </summary>
    /// <param name="ids">Ids of the filters to remove.</param>
    private void RegexRemoveFiltersById(List<int> ids)
    {
        var toRemoveTargets = new List<AutomodTarget>();
        foreach (var (target, regexFilters) in _regexFilters)
        {
            var toRemove = new List<RegexAutomodFilterDef>();
            foreach (var regexFilter in regexFilters)
            {
                if (regexFilter.Filter.Id == null || !ids.Contains(regexFilter.Filter.Id.Value))
                    continue;

                toRemove.Add(regexFilter);
            }
            foreach (var regex in toRemove)
            {
                regexFilters.Remove(regex);
            }

            if (regexFilters.Count == 0)
                toRemoveTargets.Add(target);
        }

        foreach (var target in toRemoveTargets)
        {
            _regexFilters.Remove(target);
        }
    }

    #endregion
}
