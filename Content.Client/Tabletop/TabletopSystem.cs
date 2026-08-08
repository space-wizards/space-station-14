using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopSystem : SharedTabletopSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    protected override void DragUpdated(Entity<TabletopDraggableComponent> ent)
    {
        UpdateDraggableAppearance(ent);
    }

    #region Event handlers
    /// <summary>
    /// Hologram handler: sets up the hologram to mimic another entity's sprite.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHologramAppearanceChange(Entity<TabletopHologramComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (!Appearance.TryGetData<string>(ent, TabletopItemVisuals.Prototype, out var protoId, args.Component)
            || ent.Comp.LastPrototype == protoId)
            return;

        ent.Comp.LastPrototype = protoId;

        if (ProtoMan.TryIndex(protoId, out var proto)
            && proto.TryComp<SpriteComponent>(out var protoSprite, Factory))
        {
            // HACK: we don't actually have an entity to pass, but the first parameter here is unused.
            var outSprite = CopyComp(EntityUid.Invalid, ent, protoSprite);
            outSprite.NoRotation = true;
        }

        if (!DraggableQuery.TryComp(ent, out var draggable))
            return;

        UpdateDraggableAppearance((ent, draggable), args.Sprite);
    }

    [SubscribeLocalEvent]
    private void OnDraggableStartup(Entity<TabletopDraggableComponent> ent, ref ComponentStartup _)
    {
        UpdateDraggableAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnDraggableAfterAutoHandleState(Entity<TabletopDraggableComponent> ent, ref AfterAutoHandleStateEvent _)
    {
        UpdateDraggableAppearance(ent);
    }

    private void UpdateDraggableAppearance(Entity<TabletopDraggableComponent> ent, SpriteComponent? sprite = null)
    {
        if (sprite == null && !_spriteQuery.TryComp(ent, out sprite))
            return;

        _sprite.SetScale((ent, sprite), ent.Comp.DraggingPlayer == null ? ent.Comp.NormalScale : ent.Comp.DraggedScale);
        _sprite.SetDrawDepth((ent, sprite), ent.Comp.DraggingPlayer == null ? ent.Comp.NormalDrawDepth : ent.Comp.DraggedDrawDepth);
    }

    [SubscribeLocalEvent]
    private void OnGameAutoHandleState(Entity<TabletopGameComponent> ent, ref AfterAutoHandleStateEvent _)
    {
        if (UI.TryGetOpenUi(ent.Owner, TabletopGameUiKey.Key, out var bui))
            bui.Update();
    }
    #endregion
}
