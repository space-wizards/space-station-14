using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestListing : BoxContainer
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        var entryDict = new Dictionary<string, List<CrewManifestEntry>>();

        foreach (var entry in entries.Entries)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department.ID).Add(entry);
                }
            }
        }

        var entryList = new List<(string section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        var sortOrder = _configManager.GetCVar(CCVars.CrewManifestOrdering).Split(",").ToList();

        entryList.Sort((a, b) =>
        {
            var ai = sortOrder.IndexOf(a.section);
            var bi = sortOrder.IndexOf(b.section);

            // this is up here so -1 == -1 occurs first
            if (ai == bi)
                return 0;

            if (ai == -1)
                return -1;

            if (bi == -1)
                return 1;

            return ai.CompareTo(bi);
        });

        foreach (var item in entryList)
        {
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries));
        }
    }
}
