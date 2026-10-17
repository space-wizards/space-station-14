using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    /// <summary>
    ///     The list of words to be masked in the chatbox.
    /// </summary>
    private readonly List<Regex> _filters = new();

    /// <summary>
    ///     The string holding the special symbol used to mask words.
    /// </summary>
    private string? _filterSymbol;

    public event Action<string>? WordFiltersUpdated;

    private void InitializeFilters()
    {
        _config.OnValueChanged(CCVars.ChatWordFiltersSymbol, (value) => { _filterSymbol = value; }, true);

        // Load filters if any were saved.
        var filters = _config.GetCVar(CCVars.ChatWordFilters);

        if (!string.IsNullOrEmpty(filters))
        {
            UpdateWordFilters(filters, true);
        }
    }


    public void UpdateWordFilters(string newFilters, bool firstLoad = false)
    {
        // Do nothing if the provided filters are the same as the old ones and it is not the first time.
        if (!firstLoad && _config.GetCVar(CCVars.ChatWordFilters).Equals(newFilters, StringComparison.CurrentCultureIgnoreCase))
            return;

        _config.SetCVar(CCVars.ChatWordFilters, newFilters);
        _config.SaveToFile();

        ReloadWordFilters();
        WordFiltersUpdated?.Invoke(newFilters);
    }

    public void ReloadWordFilters()
    {
        _filters.Clear();

        var filters = _config.GetCVar(CCVars.ChatWordFilters);

        // We first subdivide the filters based on newlines to prevent replacing
        // a valid "\n" tag and adding it to the final regex.
        var splittedFilters = filters.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Array.Sort(splittedFilters, (x, y) => y.Length.CompareTo(x.Length));

        for (var i = 0; i < splittedFilters.Length; i++)
        {
            // Replace every "\" character with a "\\" to prevent "\n", "\0", etc...
            var keyword = splittedFilters[i].Replace(@"\", @"\\");

            // Escape the keyword to prevent special characters like "(" and ")" to be considered valid regex.
            keyword = Regex.Escape(keyword);

            // 1. Since the "["s in WrappedMessage are already sanitized, add 2 extra "\"s
            // to make sure it matches the literal "\" before the square bracket.
            keyword = keyword.Replace(@"\[", @"\\\[");

            // If present, replace the double quotes at the edges with tags
            // that make sure the words to match are separated by spaces or punctuation.
            // NOTE: The reason why we don't use \b tags is that \b doesn't match reverse slash characters "\" so
            // a pre-sanitized (see 1.) string like "\[test]" wouldn't get picked up by the \b.
            if (keyword.Any(c => c == '"'))
            {
                // Matches the last double quote character.
                keyword = StartDoubleQuote.Replace(keyword, "(?!\\w)");
                // When matching for the first double quote character we also consider the possibility
                // of the double quote being preceded by a @ character.
                keyword = EndDoubleQuote.Replace(keyword, "(?<!\\w)");
            }

            _filters.Add(new Regex("(?i)(" + keyword + ")(?-i)(?![^[]*])"));
        }
    }
}