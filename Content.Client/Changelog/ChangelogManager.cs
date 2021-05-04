using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

#nullable enable

namespace Content.Client.Changelog
{
    public sealed class ChangelogManager
    {
        // If you fork SS14, change this to have the changelog "last seen" date stored separately.
        public const string ForkId = "Wizards";

        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

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

            using var file = _resource.UserData.Create(new ResourcePath($"/changelog_last_seen_{ForkId}"));
            using var sw = new StreamWriter(file);

            sw.Write(MaxId.ToString());
        }

        public async void Initialize()
        {
            // Open changelog purely to compare to the last viewed date.
            var changelog = await LoadChangelog();

            if (changelog.Count == 0)
            {
                return;
            }

            MaxId = changelog.Max(c => c.Id);

            var path = new ResourcePath($"/changelog_last_seen_{ForkId}");
            if (_resource.UserData.Exists(path))
            {
                LastReadId = int.Parse(_resource.UserData.ReadAllText(path));
            }

            NewChangelogEntries = LastReadId < MaxId;

            NewChangelogEntriesChanged?.Invoke();
        }

        public Task<List<ChangelogEntry>> LoadChangelog()
        {
            return Task.Run(() =>
            {
                var yamlData = _resource.ContentFileReadYaml(new ResourcePath("/Changelog/Changelog.yml"));

                if (yamlData.Documents.Count == 0)
                    return new List<ChangelogEntry>();

                var node = (MappingDataNode)yamlData.Documents[0].RootNode.ToDataNode();
                return _serialization.ReadValueOrThrow<List<ChangelogEntry>>(node["Entries"]);
            });
        }

        [DataDefinition]
        public sealed class ChangelogEntry : ISerializationHooks
        {
            [DataField("id")]
            public int Id { get; private set; }

            [DataField("author")]
            public string Author { get; } = "";

            [DataField("time")] private string _time = default!;

            public DateTime Time { get; private set; }

            [DataField("changes")]
            public List<ChangelogChange> Changes { get; } = default!;

            void ISerializationHooks.AfterDeserialization()
            {
                Time = DateTime.Parse(_time, null, DateTimeStyles.RoundtripKind);
            }
        }

        [DataDefinition]
        public sealed class ChangelogChange : ISerializationHooks
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
