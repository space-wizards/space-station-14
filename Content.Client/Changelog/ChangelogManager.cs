using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

#nullable enable

namespace Content.Client.Changelog
{
    public sealed class ChangelogManager
    {
        // If you fork SS14, change this to have the changelog "last seen" date stored separately.
        public const string ForkId = "Wizards";

        [Dependency] private readonly IResourceManager _resource = default!;

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

                var serializer = YamlObjectSerializer.NewReader((YamlMappingNode) yamlData.Documents[0].RootNode);

                return serializer.ReadDataField<List<ChangelogEntry>>("Entries");
            });
        }


        public sealed class ChangelogEntry : IExposeData
        {
            public int Id { get; private set; }
            public string Author { get; private set; } = "";
            public DateTime Time { get; private set; }
            public List<ChangelogChange> Changes { get; private set; } = default!;

            void IExposeData.ExposeData(ObjectSerializer serializer)
            {
                Id = serializer.ReadDataField<int>("id");
                Author = serializer.ReadDataField<string>("author");
                Time = DateTime.Parse(serializer.ReadDataField<string>("time"), null, DateTimeStyles.RoundtripKind);
                Changes = serializer.ReadDataField<List<ChangelogChange>>("changes");
            }
        }

        public sealed class ChangelogChange : IExposeData
        {
            public ChangelogLineType Type { get; private set; }
            public string Message { get; private set; } = "";

            void IExposeData.ExposeData(ObjectSerializer serializer)
            {
                Type = serializer.ReadDataField<ChangelogLineType>("type");
                Message = serializer.ReadDataField<string>("message");
            }
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
