using System.Linq;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter;

public abstract class SharedSprayPainterSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;

    public List<AirlockStyle> Styles { get; private set; } = new();
    public List<AirlockGroupPrototype> Groups { get; private set; } = new();

    [ValidatePrototypeId<AirlockDepartmentsPrototype>]
    private const string Departments = "Departments";

    public override void Initialize()
    {
        base.Initialize();

        // collect every style's name
        var names = new SortedSet<string>();
        foreach (AirlockGroupPrototype grp in _prototypeManager.EnumeratePrototypes<AirlockGroupPrototype>())
        {
            Groups.Add(grp);
            foreach (string style in grp.StylePaths.Keys)
            {
                names.Add(style);
            }
        }

        // get their department ids too for the final style list
        var departments = _prototypeManager.Index<AirlockDepartmentsPrototype>(Departments);
        Styles = new List<AirlockStyle>(names.Count);
        foreach (var name in names)
        {
            departments.Departments.TryGetValue(name, out var department);
            Styles.Add(new AirlockStyle(name, department));
        }
    }
}

public record struct AirlockStyle(string Name, string? Department);
