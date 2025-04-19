using System.Linq;
using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SprayPainter;

public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public Dictionary<string, List<SprayPainterEntry>> Entries { get; private set; } = new();
    public List<SprayPainterDecalEntry> Decals = [];

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
                    .FindAll(x => x.Styles.ContainsKey(style))
                    .MaxBy(x => x.IconPriority);

                if (group == null ||
                    !group.Styles.TryGetValue(style, out var protoId) ||
                    !_prototypeManager.TryIndex(protoId, out var proto))
                {
                    Entries[category].Add(new SprayPainterEntry(style, null));
                    continue;
                }

                Entries[category].Add(new SprayPainterEntry(style, proto));
            }
        }

        foreach (var decalPrototype in _prototypeManager.EnumeratePrototypes<DecalPrototype>().OrderBy(x => x.ID))
        {
            if (!decalPrototype.Tags.Contains("station") && !decalPrototype.Tags.Contains("markings"))
                continue;

            Decals.Add(new SprayPainterDecalEntry(decalPrototype.ID, decalPrototype.Sprite));
        }
    }
}

public sealed class SprayPainterEntry(string name, EntityPrototype? proto)
{
    public string Name = name;
    public EntityPrototype? Proto = proto;
}

public sealed class SprayPainterDecalEntry(string name, SpriteSpecifier spriteSpecifier)
{
    public string Name = name;
    public SpriteSpecifier Sprite = spriteSpecifier;
}
