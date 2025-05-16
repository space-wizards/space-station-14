using System.Linq;
using System.Text.RegularExpressions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Content.Shared.CCVar;
using Content.Client.CharacterInfo;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// A partial class of ChatUIController that handles the saving and loading of highlights for the chatbox.
/// It also makes use of the CharacterInfoSystem to optionally generate highlights based on the character's info.
/// </summary>
public sealed partial class ChatUIController : IOnSystemChanged<CharacterInfoSystem>
{
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    /// <summary>
    ///     The list of words to be highlighted in the chatbox.
    /// </summary>
    private List<string> _highlights = new();

    /// <summary>
    ///     The string holding the hex color used to highlight words.
    /// </summary>
    private string? _highlightsColor;

    private bool _autoFillHighlightsEnabled;

    /// <summary>
    ///     The boolean that keeps track of the 'OnCharacterUpdated' event, whenever it's a player attaching or opening the character info panel.
    /// </summary>
    private bool _charInfoIsAttach = false;

    public event Action<string>? HighlightsUpdated;

    private void InitializeHighlights()
    {
        _config.OnValueChanged(CCVars.ChatAutoFillHighlights, (value) => { _autoFillHighlightsEnabled = value; }, true);

        _config.OnValueChanged(CCVars.ChatHighlightsColor, (value) => { _highlightsColor = value; }, true);

        // Load highlights if any were saved.
        string highlights = _config.GetCVar(CCVars.ChatHighlights);

        if (!string.IsNullOrEmpty(highlights))
        {
            UpdateHighlights(highlights, true);
        }
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += OnCharacterUpdated;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= OnCharacterUpdated;
    }

    private void UpdateAutoFillHighlights()
    {
        if (!_autoFillHighlightsEnabled)
            return;

        // If auto highlights are enabled generate a request for new character info
        // that will be used to determine the highlights.
        _charInfoIsAttach = true;
        _characterInfo.RequestCharacterInfo();
    }

    public void UpdateHighlights(string newHighlights, bool firstLoad = false)
    {
        // Do nothing if the provided highlights are the same as the old ones and it is not the first time.
        if (!firstLoad && _config.GetCVar(CCVars.ChatHighlights).Equals(newHighlights, StringComparison.CurrentCultureIgnoreCase))
            return;

        _config.SetCVar(CCVars.ChatHighlights, newHighlights);
        _config.SaveToFile();

        _highlights.Clear();

        // We first subdivide the highlights based on newlines to prevent replacing
        // a valid "\n" tag and adding it to the final regex.
        string[] splittedHighlights = newHighlights.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < splittedHighlights.Length; i++)
        {
            // Replace every "\" character with a "\\" to prevent "\n", "\0", etc...
            string keyword = splittedHighlights[i].Replace("\\", "\\\\");

            // Escape the keyword to prevent special characters like "(" and ")" to be considered valid regex.
            keyword = Regex.Escape(keyword);

            // Replace any double quote (") character with a whole-word (\b) regex tag,
            // this tag will make sure the words to match are separated by spaces or punctuation.
            keyword = keyword.Replace("\"", "\\b");

            // Make sure any name tagged as ours gets highlighted only when others say it.
            keyword = keyword.Replace("@", "(?<=(?<=/name.*)|(?<=,.*\"\".*))");

            _highlights.Add(keyword);
        }

        // Arrange the list of highlights in descending order so that when highlighting,
        // the full word (eg. "Security") gets picked before the abbreviation (eg. "Sec").
        _highlights.Sort((x, y) => y.Length.CompareTo(x.Length));
    }

    private void OnCharacterUpdated(CharacterData data)
    {
        // If _charInfoIsAttach is false then the opening of the character panel was the one
        // to generate the event, dismiss it.
        if (!_charInfoIsAttach)
            return;

        var (_, job, _, _, entityName) = data;

        // Mark this entity's name as our character name for the "UpdateHighlights" function.
        string newHighlights = "@" + entityName;

        // Subdivide the character's name based on spaces or hyphens so that every word gets highlighted.
        if (newHighlights.Count(c => (c == ' ' || c == '-')) == 1)
            newHighlights = newHighlights.Replace("-", "\n@").Replace(" ", "\n@");

        // If the character has a name with more than one hyphen assume it is a lizard name and extract the first and
        // last name eg. "Eats-The-Food" -> "@Eats" "@Food"
        if (newHighlights.Count(c => c == '-') > 1)
            newHighlights = newHighlights.Split('-')[0] + "\n@" + newHighlights.Split('-')[^1];

        // Convert the job title to kebab-case and use it as a key for the loc file.
        string jobKey = job.Replace(' ', '-').ToLower();

        if (Loc.TryGetString($"highlights-{jobKey}", out var jobMatches))
            newHighlights += '\n' + jobMatches.Replace(", ", "\n");

        UpdateHighlights(newHighlights);
        HighlightsUpdated?.Invoke(newHighlights);
        _charInfoIsAttach = false;
    }
}
