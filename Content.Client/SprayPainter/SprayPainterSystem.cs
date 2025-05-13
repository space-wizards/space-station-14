using System.Linq;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.SprayPainter;

/// <summary>
/// Responsible for caching info for the spray painter menu.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public Dictionary<string, List<SprayPainterEntry>> Entries { get; private set; } = new();
    public List<SprayPainterDecalEntry> Decals = [];

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<SprayPainterComponent>(ent => new StatusControl(ent));

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

    private sealed class StatusControl : Control
    {
        private readonly RichTextLabel _label;
        private readonly Entity<SprayPainterComponent> _entity;
        private bool? _lastPaintingDecals = null;

        public StatusControl(Entity<SprayPainterComponent> ent)
        {
            _entity = ent;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_entity.Comp.IsPaintingDecals == _lastPaintingDecals)
                return;

            _lastPaintingDecals = _entity.Comp.IsPaintingDecals;

            var modeLocString = _entity.Comp.IsPaintingDecals
                ? "spray-painter-item-status-enabled"
                : "spray-painter-item-status-disabled";

            _label.SetMarkupPermissive(Robust.Shared.Localization.Loc.GetString("spray-painter-item-status-label",
                ("mode", Robust.Shared.Localization.Loc.GetString(modeLocString))));
        }
    }
}

/// <summary>
/// A spray paintable object, mapped by arbitary key.
/// </summary>
public sealed record SprayPainterEntry(string Name, EntProtoId? Proto);

/// <summary>
/// A spray paintable decal, mapped by ID.
/// </summary>
public sealed record SprayPainterDecalEntry(string Name, SpriteSpecifier Sprite);
