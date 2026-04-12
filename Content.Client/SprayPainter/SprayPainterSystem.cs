using System.Linq;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.SprayPainter;

/// <summary>
/// Client-side spray painter functions. Caches information for spray painter windows and updates the UI to reflect component state.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public List<SprayPainterDecalEntry> Decals = [];
    public Dictionary<string, List<string>> PaintableGroupsByCategory = new();
    public Dictionary<string, Dictionary<string, EntProtoId>> PaintableStylesByGroup = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<SprayPainterComponent>(ent => new StatusControl(ent));
        SubscribeLocalEvent<SprayPainterComponent, AfterAutoHandleStateEvent>(OnStateUpdate);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CachePrototypes();
    }

    private void OnStateUpdate(Entity<SprayPainterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<SprayPainterComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SprayPainterUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<PaintableGroupCategoryPrototype>() || !args.WasModified<PaintableGroupPrototype>() || !args.WasModified<DecalPrototype>())
            return;

        CachePrototypes();
    }

    private void CachePrototypes()
    {
        PaintableGroupsByCategory.Clear();
        PaintableStylesByGroup.Clear();
        foreach (var category in Proto.EnumeratePrototypes<PaintableGroupCategoryPrototype>().OrderBy(x => x.ID))
        {
            var groupList = new List<string>();
            foreach (var groupId in category.Groups)
            {
                if (!Proto.Resolve(groupId, out var group))
                    continue;

                groupList.Add(groupId);
                PaintableStylesByGroup[groupId] = group.Styles;
            }

            if (groupList.Count > 0)
                PaintableGroupsByCategory[category.ID] = groupList;
        }

        Decals.Clear();
        foreach (var decalPrototype in Proto.EnumeratePrototypes<DecalPrototype>().OrderBy(x => x.ID))
        {
            if (!decalPrototype.Tags.Contains("station")
                && !decalPrototype.Tags.Contains("markings")
                || decalPrototype.Tags.Contains("dirty"))
                continue;

            Decals.Add(new SprayPainterDecalEntry(decalPrototype.ID, decalPrototype.Sprite));
        }
    }

    private sealed class StatusControl : Control
    {
        private readonly RichTextLabel _label;
        private readonly Entity<SprayPainterComponent> _entity;
        private DecalPaintMode? _lastPaintingDecals = null;

        public StatusControl(Entity<SprayPainterComponent> ent)
        {
            _entity = ent;
            _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_entity.Comp.DecalMode == _lastPaintingDecals)
                return;

            _lastPaintingDecals = _entity.Comp.DecalMode;

            string modeLocString = _entity.Comp.DecalMode switch
            {
                DecalPaintMode.Add => "spray-painter-item-status-add",
                DecalPaintMode.Remove => "spray-painter-item-status-remove",
                _ => "spray-painter-item-status-off"
            };

            _label.SetMarkupPermissive(Robust.Shared.Localization.Loc.GetString("spray-painter-item-status-label",
                ("mode", Robust.Shared.Localization.Loc.GetString(modeLocString))));
        }
    }
}

/// <summary>
/// A spray paintable decal, mapped by ID.
/// </summary>
public sealed record SprayPainterDecalEntry(string Name, SpriteSpecifier Sprite);
