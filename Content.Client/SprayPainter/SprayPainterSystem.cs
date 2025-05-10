using System.Linq;
using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SprayPainter;

/// <summary>
/// Responsible for preparing the data for presentable appearance in the spray painter menu.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public Dictionary<string, List<SprayPainterEntry>> Entries { get; private set; } = new();
    public List<SprayPainterDecalEntry> Decals = [];

    public override void Initialize()
    {
        base.Initialize();

        CachePrototypes();
    }

    protected override void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        base.OnPrototypesReloaded(args);

        if (!args.WasModified<PaintableGroupPrototype>())
            return;

        CachePrototypes();
    }

    private void CachePrototypes()
    {
        foreach (var category in Targets.Keys)
        {
            var target = Targets[category];
            Entries.Add(category, new());

            foreach (var style in target.Styles)
            {
                var group = target.Groups
                    .FindAll(x => x.Styles.ContainsKey(style))
                    .MaxBy(x => x.IconPriority);

                if (group == null ||
                    !group.Styles.TryGetValue(style, out var protoId))
                {
                    Entries[category].Add(new SprayPainterEntry(style, null));
                    continue;
                }

                Entries[category].Add(new SprayPainterEntry(style, protoId));
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

/// <summary>
/// Used for convenient data storage.
/// </summary>
public sealed record SprayPainterEntry(string Name, EntProtoId? Proto);

/// <summary>
/// Used for convenient data storage.
/// </summary>
public sealed record SprayPainterDecalEntry(string Name, SpriteSpecifier Sprite);
