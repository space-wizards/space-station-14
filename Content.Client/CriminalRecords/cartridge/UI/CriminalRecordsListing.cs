using Content.Shared.CriminalRecords;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CriminalRecords.UI;

public sealed class CriminalRecordsListing : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CriminalRecordsListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCriminalRecords(IEnumerable<(uint, CriminalRecord)> records)
    {
        foreach (var record in records)
        {
            AddChild(new Label()
            {
                StyleClasses = { "LabelBig" },
                Text = "test"
            });
        }

        // var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        // foreach (var entry in entries.Entries)
        // {
        //     foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        //     {
        //         // this is a little expensive, and could be better
        //         if (department.Roles.Contains(entry.JobPrototype))
        //         {
        //             entryDict.GetOrNew(department).Add(entry);
        //         }
        //     }
        // }

        // var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        // foreach (var (section, listing) in entryDict)
        // {
        //     entryList.Add((section, listing));
        // }

        // entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        // foreach (var item in entryList)
        // {
        //     AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries));
        // }
    }
}
