using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.Client.Changelog
{
    public sealed partial class ChangelogManager : IPostInjectInit
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private const string SawmillName = "changelog";
        public const string MainChangelogName = "Changelog";

        private ISawmill _sawmill = default!;

        public bool NewChangelogEntries { get; private set; }
        public int LastReadId { get; private set; }
        public int MaxId { get; private set; }

        public event Action? NewChangelogEntriesChanged;

        /// <summary>
        ///     Ran when the user opens ("read") the changelog,
        ///     stores the new ID to disk and clears <see cref="NewChangelogEntries"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="LastReadId"/> is NOT cleared
        ///     since that's used in the changelog menu to show the "since you last read" bar.
        /// </remarks>
        public void SaveNewReadId()
        {
            NewChangelogEntries = false;
            NewChangelogEntriesChanged?.Invoke();

            using var sw = _resource.UserData.OpenWriteText(new ($"/changelog_last_seen_{_configManager.GetCVar(CCVars.ServerId)}"));

            sw.Write(MaxId.ToString());
        }

        public async void Initialize()
        {
            // Open changelog purely to compare to the last viewed date.
            var changelogs = await LoadChangelog();
            UpdateChangelogs(changelogs);
        }

        private void UpdateChangelogs(List<Changelog> changelogs)
        {
            if (changelogs.Count == 0)
            {
                return;
            }

            var mainChangelogs = changelogs.Where(c => c.Name == MainChangelogName).ToArray();
            if (mainChangelogs.Length == 0)
            {
                _sawmill.Error($"No changelog file found in Resources/Changelog with name {MainChangelogName}");
                return;
            }

            var changelog = changelogs[0];
            if (mainChangelogs.Length > 1)
            {
                _sawmill.Error($"More than one file found in Resource/Changelog with name {MainChangelogName}");
            }

            if (changelog.Entries.Count == 0)
            {
                return;
            }

            MaxId = changelog.Entries.Max(c => c.Id);

            var path = new ResPath($"/changelog_last_seen_{_configManager.GetCVar(CCVars.ServerId)}");
            if (_resource.UserData.TryReadAllText(path, out var lastReadIdText))
            {
                LastReadId = int.Parse(lastReadIdText);
            }

            NewChangelogEntries = LastReadId < MaxId;

            NewChangelogEntriesChanged?.Invoke();
        }

        public Task<List<Changelog>> LoadChangelog()
        {
            return Task.Run(() =>
            {
                var changelogs = new List<Changelog>();
                var directory = new ResPath("/Changelog");
                foreach (var file in _resource.ContentFindFiles(new ResPath("/Changelog/")))
                {
                    if (file.Directory != directory || file.Extension != "yml")
                        continue;

                    var yamlData = _resource.ContentFileReadYaml(file);

                    if (yamlData.Documents.Count == 0)
                        continue;

                    var node = yamlData.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
                    var changelog = _serialization.Read<Changelog>(node, notNullableOverride: true);
                    if (string.IsNullOrWhiteSpace(changelog.Name))
                        changelog.Name = file.FilenameWithoutExtension;

                    changelogs.Add(changelog);
                }

                changelogs.Sort((a, b) => a.Order.CompareTo(b.Order));
                return changelogs;
            });
        }

        public void PostInject()
        {
            _sawmill = _logManager.GetSawmill(SawmillName);
        }

        /// <summary>
        ///     Tries to return a human-readable version number from the build.json file
        /// </summary>
        public string GetClientVersion()
        {
            var fork = _configManager.GetCVar(CVars.BuildForkId);
            var version = _configManager.GetCVar(CVars.BuildVersion);

            // This trimming might become annoying if down the line some codebases want to switch to a real
            // version format like "104.11.3" while others are still using the git hashes
            if (version.Length > 7)
                version = version[..7];

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(fork))
                return Loc.GetString("changelog-version-unknown");

            return Loc.GetString("changelog-version-tag",
                ("fork", fork),
                ("version", version));
        }

        [DataDefinition]
        public sealed partial class Changelog
        {
            /// <summary>
            ///     The name to use for this changelog.
            ///     If left unspecified, the name of the file is used instead.
            ///     Used during localization to find the user-displayed name of this changelog.
            /// </summary>
            [DataField("Name")]
            public string Name = string.Empty;

            /// <summary>
            ///     The individual entries in this changelog.
            ///     These are not kept around in memory in the changelog manager.
            /// </summary>
            [DataField("Entries")]
            public List<ChangelogEntry> Entries = new();

            /// <summary>
            ///     Whether or not this changelog will be displayed as a tab to non-admins.
            ///     These are still loaded by all clients, but not shown if they aren't an admin,
            ///     as they do not contain sensitive data and are publicly visible on GitHub.
            /// </summary>
            [DataField("AdminOnly")]
            public bool AdminOnly;

            /// <summary>
            ///     Used when ordering the changelog tabs for the user to see.
            ///     Larger numbers are displayed later, smaller numbers are displayed earlier.
            /// </summary>
            [DataField("Order")]
            public int Order;
        }

        [DataDefinition]
        public sealed partial class ChangelogEntry
        {
            [DataField("id")]
            public int Id { get; private set; }

            [DataField("author")]
            public string Author { get; private set; } = "";

            [DataField]
            public DateTime Time { get; private set; }

            [DataField("changes")]
            public List<ChangelogChange> Changes { get; private set; } = default!;
        }

        [DataDefinition]
        public sealed partial class ChangelogChange
        {
            [DataField("type")]
            public ChangelogLineType Type { get; private set; }

            [DataField("message")]
            public string Message { get; private set; } = "";
        }

        public enum ChangelogLineType
        {
            Add,
            Remove,
            Fix,
            Tweak,
        }
    }
}
