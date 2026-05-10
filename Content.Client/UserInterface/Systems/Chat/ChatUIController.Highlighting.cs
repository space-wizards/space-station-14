using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Content.Shared.CCVar;
using Content.Client.CharacterInfo;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// A partial class of ChatUIController that handles the saving and loading of highlights for the chatbox.
/// It also makes use of the CharacterInfoSystem to optionally generate highlights based on the character's info.
/// </summary>
public sealed partial class ChatUIController : IOnSystemChanged<CharacterInfoSystem>
{
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private  ISerializationManager _serial = default!;
    [UISystemDependency] private CharacterInfoSystem _characterInfo = default!;

    /// <summary>
    ///     The path to the highlights YAML file.
    ///     This is stored in UserData alongside keybinds and client_config.
    /// </summary>
    private static readonly ResPath HighlightsPath = new("/highlights.yml");

    /// <summary>
    ///     The "ServerId" name to use as a default profile when not using per-server settings.
    /// </summary>
    private static readonly string DefaultProfile = "Default";

    private static readonly Regex StartDoubleQuote = new("\"$");
    private static readonly Regex EndDoubleQuote = new("^\"|(?<=^@)\"");
    private static readonly Regex StartAtSign = new("^@");

    /// <summary>
    ///     The list of words to be highlighted in the chatbox.
    /// </summary>
    private readonly List<string> _highlights = new();

    /// <summary>
    ///     A cache of all highlights from the YAML file. This loads all highlights from all servers.
    ///     The memory overhead to store these should be reasonably negligible.
    /// </summary>
    private List<HighlightProto>? _allHighlights;

    /// <summary>
    ///     The human-friendly string of words to be highlighted in the chatbox.
    /// </summary>
    public string CurrentHighlightString { get; private set; } = string.Empty;

    /// <summary>
    ///     The string holding the hex color used to highlight words.
    ///     Local cache of CVar.
    /// </summary>
    private string? _highlightsColor;

    /// <summary>
    ///     The string holding highlights generated from character data (e.g. Job, Name).
    /// </summary>
    private string _characterHighlights = string.Empty;

    /// <summary>
    ///     The string holding the current set of user-defined persistent highlights.
    ///     Local cache of CVar.
    /// </summary>
    private string _persistentHighlights = string.Empty;

    /// <summary>
    ///     A string holding the current server ID.
    ///     Local cache of CVar.
    /// </summary>
    private string _serverId = DefaultProfile;

    /// <summary>
    ///     A boolean defining if highlights should be supplemented with character info.
    ///     Local cache of CVar.
    /// </summary>
    private bool _autoFillHighlightsEnabled;

    /// <summary>
    ///     A boolean defining if highlights should be saved per-server or to the default profile.
    ///     Local cache of CVar.
    /// </summary>
    private bool _perServerHighlightsEnabled;

    /// <summary>
    ///     The boolean that keeps track of the 'OnCharacterUpdated' event, whenever it's a player attaching or opening the character info panel.
    /// </summary>
    private bool _charInfoIsAttach = false;

    /// <summary>
    ///     An event raised when highlights have been updated.
    ///     Provides a string containing the new highlights.
    ///  </summary>
    public event Action<string>? HighlightsUpdated;

    private void InitializeHighlights()
    {
        _config.OnValueChanged(CCVars.ServerId,
            (value)
                =>
            {
                _serverId = value;
                if (_perServerHighlightsEnabled)
                    _config.SetCVar(CCVars.ChatPersistentHighlights, GetPersistentHighlights(value));
            },
            true);

        _config.OnValueChanged(CCVars.ChatAutoFillHighlights,
            (value)
                =>
            {
                _autoFillHighlightsEnabled = value;
                UpdateHighlights("", true);
            },
            true);

        _config.OnValueChanged(CCVars.ChatHighlightsColor,
            (value)
                =>
            {
                _highlightsColor = value;
            },
            true);

        _config.OnValueChanged(CCVars.ChatPersistentHighlights,
            (value)
                =>
            {
                _persistentHighlights = value;
                SaveToUserData(HighlightsPath,
                    value,
                    _perServerHighlightsEnabled ? _serverId : DefaultProfile);
                UpdateHighlights("", true);
            },
            true);

        _config.OnValueChanged(CCVars.ChatPerServerHighlights,
            (value)
                =>
             {
                _perServerHighlightsEnabled = value;
                _config.SetCVar(CCVars.ChatPersistentHighlights,
                    GetPersistentHighlights(value ? _serverId : DefaultProfile));
            },
            true);
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += OnCharacterUpdated;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= OnCharacterUpdated;
    }

    /// <summary>
    ///     Saves the provided highlight string to local memory and disk.
    ///     This overwrites the existing file.
    /// </summary>
    /// <param name="path">ResPath for the file to save. Creates if nonexistent.</param>
    /// <param name="highlights">String containing the newline separated highlights to be saved.</param>
    /// <param name="serverId">String containing ServerId to save to.</param>
    private void SaveToUserData(ResPath path, string highlights, string serverId)
    {
        // Cannot save without reading first
        if (_allHighlights is null)
            return;

        var currentServer = new HighlightProto
        {
            ServerId = serverId,
            Highlights = highlights
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToArray(),
        };

        var index = _allHighlights.FindIndex(x => x.ServerId == serverId);

        if (index < 0)
            _allHighlights.Add(currentServer);
        else if (!Enumerable.SequenceEqual(_allHighlights[index].Highlights, currentServer.Highlights))
            _allHighlights[index] = currentServer;
        else // Contents existed and didn't change
            return;

        var mapping = new MappingDataNode();

        // Version value allows format change and migration logic if necessary
        // This is not implemented whatsoever, but adding a value to the file is free
        mapping.Add("version", new ValueDataNode("1"));
        mapping.Add("persistentHighlights", _serial.WriteValue(_allHighlights, notNullableOverride: true));

        using var writer = _res.UserData.OpenWriteText(path);
        var stream = new YamlStream {new(mapping.ToYaml())};
        stream.Save(new YamlMappingFix(new Emitter(writer)), false);
    }

    /// <summary>
    ///     Retrieve the highlights for a given server.
    ///     Reads from internal store. If internal store is null, updates it from disk.
    ///     If internal store does not contain the serverId, provides an empty string.
    /// </summary>
    /// <param name="serverId">String containing ServerId to save to.</param>
    /// <returns>String containing the highlights for the requested server.</returns>
    public string GetPersistentHighlights(string serverId)
    {
        // Load from file if null
        _allHighlights ??= LoadFromUserData(HighlightsPath);

        // No highlights saved for this server yet
        if (!_allHighlights.TryFirstOrDefault(x => x.ServerId == serverId, out var serverHighlights))
            return string.Empty;

        return string.Join('\n', serverHighlights.Highlights);
    }

    /// <summary>
    ///     Loads all highlights from disk.
    /// </summary>
    /// <param name="path">ResPath to the highlights file.</param>
    /// <returns>A list of HighlightProto</returns>
    /// <exception cref="InvalidDataException">Thrown when the YAML is malformed.</exception>
    private List<HighlightProto> LoadFromUserData(ResPath path)
    {
        // No file exists yet, the file will be created when saved
        if (!_res.UserData.Exists(path))
            return new List<HighlightProto>();

        var reader = _res.UserData.OpenText(path);
        var doc = DataNodeParser.ParseYamlStream(reader);
        var map = (MappingDataNode) doc.First().Root;

        if (!map.TryGet("persistentHighlights", out SequenceDataNode? serverHighlightsNode))
            throw new InvalidDataException("Malformed highlights file, could not find persistent highlights node");

        return _serial.Read<List<HighlightProto>>(serverHighlightsNode, notNullableOverride: true);
    }

    /// <summary>
    ///     Raises a request for updated character info.
    /// </summary>
    private void UpdateAutoFillHighlights()
    {
        if (!_autoFillHighlightsEnabled)
            return;

        // If auto highlights are enabled generate a request for new character info
        // that will be used to determine the highlights.
        _charInfoIsAttach = true;
        _characterInfo.RequestCharacterInfo();
    }

    /// <summary>
    ///     Update all highlights and raises <see cref="HighlightsUpdated"/>.
    /// </summary>
    /// <param name="newHighlights">A string containing the highlights to apply. Ignored if regenerate is true.</param>
    /// <param name="regenerate">Boolean determining if highlights will be regenerated from source,
    /// or if the newHighlights string will be applied.</param>
    public void UpdateHighlights(string newHighlights, bool regenerate = false)
    {
        // Do nothing if the provided highlights are the same as the old ones and it is not forced to refresh.
        if (!regenerate && CurrentHighlightString.Equals(newHighlights, StringComparison.CurrentCultureIgnoreCase))
            return;

        var rawHighlights = newHighlights;

        // Regenerate highlights from Character (if enabled) and Persistent
        if (regenerate)
        {
            // Ensure trailing newline
            rawHighlights = _persistentHighlights.EndsWith('\n')
                ? _persistentHighlights
                : _persistentHighlights + '\n';

            if (_autoFillHighlightsEnabled)
                rawHighlights += _characterHighlights;
        }

        _highlights.Clear();

        // We first subdivide the highlights based on newlines to prevent replacing
        // a valid "\n" tag and adding it to the final regex.
        // We also ensure that duplicate highlights are not added.
        var splitHighlights = rawHighlights.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();

        foreach (var highlight in splitHighlights)
        {
            // Replace every "\" character with a "\\" to prevent "\n", "\0", etc...
            var keyword = highlight.Replace(@"\", @"\\");

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

            // Make sure the character's name is highlighted only when mentioned directly (eg. it's said by someone),
            // for example in 'Name Surname says, "..."' 'Name Surname' won't be highlighted.
            keyword = StartAtSign.Replace(keyword, @"(?<=(?<=^.?OOC:.*:.*)|(?<=,.*"".*)|(?<=\n.*))");

            _highlights.Add(keyword);
        }

        // Arrange the list of highlights in descending order so that when highlighting,
        // the full word (eg. "Security") gets picked before the abbreviation (eg. "Sec").
        _highlights.Sort((x, y) => y.Length.CompareTo(x.Length));

        // Collapse split highlights array back to string, leveraging the cleanup and whitespace but dodging the regex.
        CurrentHighlightString = string.Join('\n', splitHighlights);

        // Update anything that wants to know highlights have changed
        HighlightsUpdated?.Invoke(CurrentHighlightString);
    }

    /// <summary>
    ///     Processes new character information.
    ///     Updates the _characterHighlights string and regenerates all highlights.
    ///     Early return if character info did not change.
    /// </summary>
    /// <param name="data">CharacterData definition</param>
    private void OnCharacterUpdated(CharacterData data)
    {
        // If _charInfoIsAttach is false then the opening of the character panel was the one
        // to generate the event, dismiss it. Then reset the flag.
        if (!_charInfoIsAttach)
            return;
        _charInfoIsAttach = false;

        var (_, job, _, _, entityName) = data;

        // Mark this entity's name as our character name for the "UpdateHighlights" function.
        var newHighlights = "@" + entityName;

        // Subdivide the character's name based on spaces or hyphens so that every word gets highlighted.
        if (newHighlights.Count(c => (c == ' ' || c == '-')) == 1)
            newHighlights = newHighlights.Replace("-", "\n@").Replace(" ", "\n@");

        // If the character has a name with more than one hyphen assume it is a lizard name and extract the first and
        // last name eg. "Eats-The-Food" -> "@Eats" "@Food"
        if (newHighlights.Count(c => c == '-') > 1)
            newHighlights = newHighlights.Split('-')[0] + "\n@" + newHighlights.Split('-')[^1];

        // Convert the job title to kebab-case and use it as a key for the loc file.
        var jobKey = job.Replace(' ', '-').ToLower();

        if (_loc.TryGetString($"highlights-{jobKey}", out var jobMatches))
            newHighlights += '\n' + jobMatches.Replace(", ", "\n");

        // Nothing new
        if (_characterHighlights.Equals(newHighlights, StringComparison.CurrentCultureIgnoreCase))
            return;

        _characterHighlights = newHighlights;

        UpdateHighlights("", true);
    }
}
