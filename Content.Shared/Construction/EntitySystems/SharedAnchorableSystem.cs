using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.EntitySystems;

public abstract class SharedAnchorableSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    protected EntityQuery<TagComponent> TagQuery;

    public const string Unstackable = "Unstackable";

    public override void Initialize()
    {
        base.Initialize();

        TagQuery = GetEntityQuery<TagComponent>();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
    }

    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !usedTool.Qualities.Contains(anchorable.Tool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    public virtual void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        // Thanks tool system.

        // TODO tool system is fixed now, make this actually shared.
    }

    public bool AnyUnstackablesAnchoredAt(EntityCoordinates location)
    {
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(grid.LocalToTile(location));

        while (enumerator.MoveNext(out var entity))
        {
            // If we find another unstackable here, return true.
            if (_tagSystem.HasTag(entity.Value, Unstackable, TagQuery))
            {
                return true;
            }
        }

        return false;
    }

    public bool AnyUnstackable(EntityUid uid, EntityCoordinates location)
    {
        // If we are unstackable, iterate through any other entities anchored on the current square
        if (_tagSystem.HasTag(uid, Unstackable, TagQuery))
        {
            return AnyUnstackablesAnchoredAt(location);
        }

        return false;
    }

    [Serializable, NetSerializable]
    protected sealed class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}
