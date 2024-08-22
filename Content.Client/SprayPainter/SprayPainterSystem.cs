using System.Linq;
using Content.Shared.SprayPainter;
using Robust.Shared.Prototypes;

namespace Content.Client.SprayPainter;

public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public Dictionary<string, List<SprayPainterEntry>> Entries { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        foreach (var category in Targets.Keys)
        {
            var target = Targets[category];
            Entries.Add(category, new());

            foreach (string style in target.Styles)
            {
                var group = target.Groups
                    .FindAll(x => x.StylePaths.ContainsKey(style))
                    .MaxBy(x => x.IconPriority);

                if (group == null ||
                    !group.StylePaths.TryGetValue(style, out var protoId) ||
                    !_prototypeManager.TryIndex(protoId, out var proto))
                {
                    Entries[category].Add(new SprayPainterEntry(style, null));
                    continue;
                }

                Entries[category].Add(new SprayPainterEntry(style, proto));
            }
        }
    }
}

public sealed class SprayPainterEntry
{
    public string Name;
    public EntityPrototype? Proto;

    public SprayPainterEntry(string name, EntityPrototype? proto)
    {
        Name = name;
        Proto = proto;
    }
}
